using System;
using EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQConnector.Connection;

namespace RabbitMQConnector.Extensions
{
    /// <summary>
    /// Service Collection Extensions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Публичные методы

        /// <summary>
        /// Добавляет шину событий для отправки сообщений
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="connectionUrl">URL для подключения к RabbitMQ</param>
        /// <param name="exchangeName">Название обменника.</param>
        /// <param name="timeoutBeforeReconnecting">
        /// Количество времени в секундах, которое приложение будет ждать после попытки
        /// подключения к RabbitMQ.
        /// </param>
        public static void AddEventBusPublisher(this IServiceCollection services, string connectionUrl,
            string exchangeName, int timeoutBeforeReconnecting = 15)
        {
            services.AddSingleton<IEventBusPublisher, EventBusPublisher>(factory =>
            {
                ILogger<EventBusPublisher> logger = factory.GetRequiredService<ILogger<EventBusPublisher>>();
                ConnectionFactory connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(connectionUrl),
                    DispatchConsumersAsync = true
                };

                return new EventBusPublisher(exchangeName,
                    new RabbitMqPersistentConnection(connectionFactory, logger,
                        timeoutBeforeReconnecting), logger);
            });
        }

        /// <summary>
        /// Добавляет шину событий для получения сообщений
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="connectionUrl">URL для подключения к RabbitMQ</param>
        /// <param name="exchangeName">Название обменника.</param>
        /// <param name="timeoutBeforeReconnecting">
        /// Количество времени в секундах, которое приложение будет ждать после попытки
        /// подключения к RabbitMQ.
        /// </param>
        public static void AddEventBusSubscriber(this IServiceCollection services, string connectionUrl,
            string exchangeName, int timeoutBeforeReconnecting = 15)
        {
            services.AddSingleton<IEventBusSubscriber, EventBusSubscriber>(factory =>
            {
                ILogger<EventBusSubscriber> logger = factory.GetRequiredService<ILogger<EventBusSubscriber>>();
                ConnectionFactory connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(connectionUrl),
                    DispatchConsumersAsync = true
                };

                return new EventBusSubscriber(exchangeName,
                    new RabbitMqPersistentConnection(connectionFactory, logger,
                        timeoutBeforeReconnecting), logger);
            });
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Добавить соединение
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="connectionUrl">Строка соединение</param>
        /// <param name="timeoutBeforeReconnecting">Интервал перед переподключением</param>
        private static void AddPersistentConnection(IServiceCollection services,
            string connectionUrl, int timeoutBeforeReconnecting = 15)
        {
            services.AddSingleton<IPersistentConnection, RabbitMqPersistentConnection>(factory =>
            {
                ConnectionFactory connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(connectionUrl),
                    DispatchConsumersAsync = true
                };

                ILogger<RabbitMqPersistentConnection> logger =
                    factory.GetService<ILogger<RabbitMqPersistentConnection>>();

                return new RabbitMqPersistentConnection(connectionFactory, logger, timeoutBeforeReconnecting);
            });
        }

        #endregion
    }
}