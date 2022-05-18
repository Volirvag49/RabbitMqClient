using System;
using EventBus.Events;
using EventBus.Interfaces;
using EventBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RabbitMQConnector.Tests
{
    /// <summary>
    /// Точка входа в приложение
    /// </summary>
    public class EntryPoint
    {
        #region Поля

        /// <summary>
        /// Подписчик
        /// </summary>
        private readonly IEventBusSubscriber _eventBusSubscriber;

        /// <summary>
        /// Отправитель
        /// </summary>
        private readonly IEventBusPublisher _eventBusPublisher;

        /// <summary>
        /// Роут публикатора сообщений
        /// </summary>
        private readonly string _publishRoute;

        /// <summary>
        /// Очередь слушателя сообщений
        /// </summary>
        private readonly string _subscribeQueue;

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="eventBusSubscriber">Подписчик</param>
        /// <param name="eventBusPublisher">Отправитель</param>
        /// <param name="configuration">IConfiguration</param>
        public EntryPoint(IEventBusSubscriber eventBusSubscriber,
            IEventBusPublisher eventBusPublisher,
            IConfiguration configuration, ILogger<EntryPoint> logger)
        {
            _eventBusSubscriber = eventBusSubscriber;
            _eventBusPublisher = eventBusPublisher;

            IConfigurationSection rabbitMqSection = configuration.GetSection("RabbitMQ");
            _publishRoute = rabbitMqSection["PublishRoute"];
            _subscribeQueue = rabbitMqSection["SubscribeQueue"];
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Запуск
        /// </summary>
        /// <param name="args">args</param>
        public void Run(string[] args)
        {
            Console.WriteLine("\n---Тестовое приложение подписывается на получения сообщений...\n");
            _eventBusSubscriber.Subscribe<SamplePayload>(_subscribeQueue, ConsumerReceived);
            Console.WriteLine("\n---Тестовое приложение успешно подписалось на получение сообщений...\n");

            SamplePayload samplePayload = new SamplePayload
            {
                Content = Guid.NewGuid().ToString()
            };

            EventBusMessage eventBusMessage =
                new EventBusMessage(samplePayload);
            Console.WriteLine("\n---Тестовое приложение публикует сообщение...\n");
            _eventBusPublisher.Publish(_publishRoute, eventBusMessage);
            Console.WriteLine("\n---Тестовое приложение успешно опубликовало сообщение...\n");

            Console.ReadLine();
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Обработка сообщения
        /// </summary>
        /// <param name="sender">Источник</param>
        /// <param name="e">событие</param>
        private void ConsumerReceived(object sender, ConsumerReceivedEventArgs e)
        {
            Console.WriteLine($"\n---Тестовое приложение получает сообщение: #{e.Message.Id}...");
            try
            {
                // логика
                SamplePayload message = e.GetPayload<SamplePayload>();

                //Если без ошибок, то...
                e.Received();
                Console.WriteLine($"\n---Тестовое приложение успешно обработало сообщение: #{e.Message.Id}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n---Тестовое приложение не смогло обработало сообщение: #{e.Message.Id}. Ошибка: {ex.Message}...");
            }
        }

        #endregion

        #region Классы

        /// <summary>
        /// Рабочая нагрузка
        /// </summary>
        public class SamplePayload
        {
            #region Свойства

            /// <summary>
            /// Контент
            /// </summary>
            public string Content { get; set; }

            #endregion
        }

        #endregion
    }
}