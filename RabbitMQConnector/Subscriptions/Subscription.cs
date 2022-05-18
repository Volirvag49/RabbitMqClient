using System;
using EventBus.Events;
using EventBus.Models;

namespace RabbitMQConnector.Subscriptions
{
    /// <summary>
    /// Модель подписки на прослушивание событий
    /// </summary>
    public class Subscription
    {
        #region События

        /// <summary>
        /// Событие, возникающие при получении сообщения
        /// </summary>
        public event EventHandler<ConsumerReceivedEventArgs>? ConsumerReceivedEvent;

        #endregion

        #region Свойства

        /// <summary>
        /// Id подписки
        /// </summary>
        public string Id { get; set; }

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="id">Идентификатор</param>
        /// <param name="consumerReceivedHandler">Обработчик события получения сообщения</param>
        public Subscription(string id, EventHandler<ConsumerReceivedEventArgs>? consumerReceivedHandler)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            ConsumerReceivedEvent += consumerReceivedHandler ??
                                     throw new ArgumentNullException(nameof(consumerReceivedHandler));
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Подписчик обработал сообщения
        /// </summary>
        /// <param name="eventBusMessage">Сообщение</param>
        /// <returns></returns>
        public bool ConsumerReceived(EventBusMessage eventBusMessage)
        {
            ConsumerReceivedEventArgs consume = new ConsumerReceivedEventArgs(eventBusMessage);
            ConsumerReceivedEvent?.Invoke(this, consume);

            return consume.IsReceived;
        }

        #endregion
    }
}