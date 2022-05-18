using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQConnector.Connection;

namespace RabbitMQConnector
{
    /// <summary>
    /// Базовый класс EventBus
    /// </summary>
    public abstract class EventBusBase
    {
        #region Поля

        /// <summary>
        /// Название обменника
        /// </summary>
        protected readonly string ExchangeName;

        /// <summary>
        /// Представляет логику подключения к шине
        /// </summary>
        protected readonly IPersistentConnection PersistentConnection;

        /// <summary>
        /// ILogger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// AMQP модель
        /// </summary>
        protected IModel? ConsumerChannel;

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="exchangeName">Название обменника</param>
        /// <param name="persistentConnection">Представляет логику подключения к шине</param>
        /// <param name="logger"></param>
        /// <Exception cref="ArgumentNullException"></Exception>
        protected EventBusBase(string exchangeName,
            IPersistentConnection persistentConnection,
            ILogger logger)
        {
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            PersistentConnection =
                persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            Logger =
                logger ?? throw new ArgumentNullException(nameof(logger));

            ConfigureMessageBroker();
        }

        #endregion

        #region Защищённые методы

        /// <summary>
        /// Инициализация RabbitMq
        /// </summary>
        /// <param name="channel">AMQP модель</param>
        /// <param name="route">Роут</param>
        protected void InitRabbitMq(IModel channel, string route)
        {
            Logger.LogTrace("Создание. очереди RabbitMQ {route}...",
                route);

            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentNullException(nameof(route));
            }

            channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct);

            channel.QueueDeclare(route, false, false, false, null);
            channel.QueueBind(route,
                ExchangeName,
                route);

            Logger.LogTrace("Создание. очереди RabbitMQ {route} прошло успешно...",
                route);
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Конфигурация брокера сообщений
        /// </summary>
        private void ConfigureMessageBroker()
        {
            ConsumerChannel = CreateConsumerChannel();
            PersistentConnection.OnReconnectedAfterConnectionFailure +=
                PersistentConnection_OnReconnectedAfterConnectionFailure;
        }

        /// <summary>
        /// Обработчик события создания канала потребителя сообщений
        /// </summary>
        /// <param name="sender">Источник</param>
        /// <param name="e">Аргументы</param>
        private void PersistentConnection_OnReconnectedAfterConnectionFailure(object sender, EventArgs e)
        {
            DoCreateConsumerChannel();
        }

        /// <summary>
        /// Создание потребителя сообщений
        /// </summary>
        private void DoCreateConsumerChannel()
        {
            ConsumerChannel.Dispose();
            ConsumerChannel = CreateConsumerChannel();
        }

        /// <summary>
        /// Создание потребительский сообщений
        /// </summary>
        /// <returns></returns>
        private IModel CreateConsumerChannel()
        {
            if (!PersistentConnection.IsConnected)
            {
                PersistentConnection.TryConnect();
            }

            Logger.LogTrace("Создание потребительского канала RabbitMQ...");

            IModel channel = PersistentConnection.CreateModel();

            channel.ExchangeDeclare(ExchangeName, "direct");

            channel.CallbackException += (sender, ea) =>
            {
                Logger.LogWarning(ea.Exception, "Создание потребительского канала RabbitMQ завершилось с ошибкой. Повторная попытка...");
                DoCreateConsumerChannel();
            };

            Logger.LogTrace("Создание потребительского канала RabbitMQ завершилось успешно.");

            return channel;
        }

        #endregion
    }
}