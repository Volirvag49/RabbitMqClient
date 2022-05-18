using System;
using System.Net.Sockets;
using System.Text;
using EventBus.Interfaces;
using EventBus.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQConnector.Connection;
using RabbitMQConnector.Enums;

namespace RabbitMQConnector
{
    /// <summary>
    /// Отправитель
    /// </summary>
    public class EventBusPublisher : EventBusBase, IEventBusPublisher
    {
        #region Поля

        /// <summary>
        /// Кол-во попыток публикации сообщений
        /// </summary>
        private const int _publishRetryCount = 5;

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="exchangeName">Название обменника</param>
        /// <param name="persistentConnection">Представляет логику подключения к шине</param>
        /// <param name="logger"></param>
        public EventBusPublisher(string exchangeName, IPersistentConnection persistentConnection,
            ILogger logger)
            : base(exchangeName, persistentConnection, logger)
        {
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Публикация сообщения
        /// </summary>
        /// <param name="route">Роут</param>
        /// <param name="message">Сообщение</param>
        public void Publish(string route, EventBusMessage message)
        {
            if (!PersistentConnection.IsConnected)
            {
                PersistentConnection.TryConnect();
            }

            string objectType = message.Payload.GetType().Name;

            RetryPolicy policy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_publishRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (Exception, timeSpan) =>
                    {
                        Logger.LogWarning(Exception,
                            "Не удалось опубликовать событие #{EventName}. Таймаут {Timeout} сек.: {ExceptionMessage}.",
                            objectType, $"{timeSpan.TotalSeconds:n1}", Exception.Message);
                    });

            Logger.LogTrace("Создание канала RabbitMQ для публикации #{EventName}...", objectType);

            using (IModel channel = PersistentConnection.CreateModel())
            {
                string messageJson = JsonConvert.SerializeObject(message);
                byte[] body = Encoding.UTF8.GetBytes(messageJson);

                policy.Execute(() =>
                {
                    IBasicProperties properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = (byte) DeliveryMode.Persistent;

                    Logger.LogInformation("Публикация события: #{Id}. Сообщение: {messageJson}...",
                        message.Id,
                        messageJson);

                    channel.BasicPublish(
                        ExchangeName,
                        route,
                        true,
                        properties,
                        body);

                    Logger.LogInformation("Событие: #{Id} опубликовано. Сообщение: {messageJson}...",
                        message.Id,
                        messageJson);
                });
            }
        }

        #endregion
    }
}