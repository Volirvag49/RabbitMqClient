using System;
using System.Text;
using System.Threading.Tasks;
using EventBus.Events;
using EventBus.Interfaces;
using EventBus.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQConnector.Connection;
using RabbitMQConnector.Subscriptions;

namespace RabbitMQConnector
{
    /// <summary>
    /// Подписчик
    /// </summary>
    public class EventBusSubscriber : EventBusBase, IEventBusSubscriber
    {
        #region Поля

        /// <summary>
        /// Интервал возврата сообщения в очередь
        /// </summary>
        protected readonly TimeSpan SubscribeRetryTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Менеджер подписок
        /// </summary>
        private readonly SubscriptionsManager _subscriptionsManager;

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="exchangeName">Название обменника</param>
        /// <param name="persistentConnection">Постоянное соединение</param>
        /// <param name="logger">ILogger</param>
        public EventBusSubscriber(string exchangeName, IPersistentConnection persistentConnection,
            ILogger logger)
            : base(exchangeName, persistentConnection, logger)
        {
            _subscriptionsManager = SubscriptionsManager.Get();
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Подписка на получение сообщений
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <param name="consumer">Потребитель сообщений</param>
        public void Subscribe<T>(string queue, EventHandler<ConsumerReceivedEventArgs> consumer)
        {
            Logger.LogInformation("Подписка на прослушивание очереди '{Queue}'...", queue);

            InitRabbitMq(ConsumerChannel, queue);

            _subscriptionsManager.Subscription<T>(queue, consumer);

            StartBasicConsume(queue);

            Logger.LogInformation("Подписка на прослушивание очереди выполнена успешно '{Queue}'...", queue);
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Запуск прослушивания сообщений
        /// </summary>
        /// <param name="queue">очередь</param>
        private void StartBasicConsume(string queue)
        {
            Logger.LogTrace("Запуск потребления сообщений RabbitMQ...");

            if (ConsumerChannel == null)
            {
                Logger.LogError("Не удалось запустить потребление сообщений RabbitMQ, так как AMQP-модель = null.");
                return;
            }

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(ConsumerChannel);
            consumer.Received += Consumer_Received;

            ConsumerChannel.BasicConsume
            (
                queue,
                false,
                consumer
            );

            Logger.LogTrace("Запуск потребления сообщений RabbitMQ выполнен успешно...");
        }

        /// <summary>
        /// Обработчик события получения сообщения
        /// </summary>
        /// <param name="sender">Источник</param>
        /// <param name="eventArgs">Аргументы</param>
        /// <returns></returns>
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            string eventName = eventArgs.RoutingKey;
            string message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            EventBusMessage eventBusMessage = JsonConvert.DeserializeObject<EventBusMessage>(message);

            bool isAcknowledged = false;

            try
            {
                isAcknowledged = ProcessEvent(eventName, eventBusMessage);

                if (isAcknowledged)
                {
                    ConsumerChannel.BasicAck(eventArgs.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Ошибка обработки сообщения: Event: '{EventName}'. {Message}.",
                    eventName, message);
            }
            finally
            {
                if (!isAcknowledged)
                {
                    await TryEnqueueMessageAgainAsync(eventArgs);
                }
            }
        }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="eventName">Название</param>
        /// <param name="eventBusMessage">Сообщение</param>
        /// <returns></returns>
        private bool ProcessEvent(string eventName, EventBusMessage eventBusMessage)
        {
            Guid? eventId = eventBusMessage.Id;

            Logger.LogInformation("Обработка события: '{EventName}' Id#{EventId}...",
                eventName, eventId);

            if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName, eventBusMessage.PayloadType))
            {
                Logger.LogInformation(
                    "Обработка события: '{EventName}' Id#{EventId} не будет выполнена, т.к. нет подписчиков на данное событие...",
                    eventName, eventId);
                return false;
            }

            Subscription? subs = _subscriptionsManager.GetSubscriptionForEvent(eventName, eventBusMessage.PayloadType);

            if (subs != null)
            {
                bool consumerReceived = subs.ConsumerReceived(eventBusMessage);

                if (consumerReceived)
                {
                    Logger.LogInformation(
                        "Обработка события: '{EventName}' Id#{EventId} успешно выполнена!",
                        eventName, eventId);
                }

                return consumerReceived;
            }

            Logger.LogInformation(
                "Обработка события: '{EventName}' Id#{EventId} не будет выполнена, т.к. нет подписчиков на данное событие...",
                eventName, eventId);
            return false;
        }

        /// <summary>
        /// Попытка возврата необработанное сообщение
        /// </summary>
        /// <param name="eventArgs">BasicDeliverEventArgs</param>
        /// <returns></returns>
        private async Task TryEnqueueMessageAgainAsync(BasicDeliverEventArgs eventArgs)
        {
            try
            {
                Logger.LogWarning("Возврат сообщения обратно в очередь после задержки: {Time} сек...",
                    $"{SubscribeRetryTime.TotalSeconds:n1}");

                await Task.Delay(SubscribeRetryTime);

                ConsumerChannel.BasicNack(eventArgs.DeliveryTag, false, true);

                Logger.LogTrace("Возврат сообщения обратно в очередь выполнен успешно.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Возврат сообщения обратно в очередь не выполнен. Ошибка: {Error}.", ex.Message);
            }
        }

        #endregion
    }
}
