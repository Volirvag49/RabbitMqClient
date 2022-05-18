using System;
using System.Collections.Generic;
using System.Linq;
using EventBus.Events;
using RabbitMQConnector.Helpers;

namespace RabbitMQConnector.Subscriptions
{
    /// <summary>
    /// Менеджер подписок
    /// </summary>
    public class SubscriptionsManager
    {
        #region Свойства

        /// <summary>
        /// Список подписок на сообщения
        /// </summary>
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        #endregion

        #region Публичные методы

        /// <summary>
        /// Получить Менеджер подписок
        /// </summary>
        /// <returns></returns>
        public static SubscriptionsManager Get()
        {
            return new SubscriptionsManager();
        }

        /// <summary>
        /// Проверка на существование подписки
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <param name="payloadType">Тип</param>
        /// <returns></returns>
        public bool HasSubscriptionsForEvent(string queue, string payloadType)
        {
            return Subscriptions.Any(q
                => q.Id == SubscriptionIdGenerator.GenerateId(queue, payloadType));
        }

        /// <summary>
        /// Получение подписки на сообщение
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <param name="payloadType">Тип</param>
        /// <returns></returns>
        public Subscription GetSubscriptionForEvent(string queue, string payloadType)
        {
            return Subscriptions.FirstOrDefault(q
                => q.Id == SubscriptionIdGenerator.GenerateId(queue, payloadType));
        }

        /// <summary>
        /// Подписка на событие
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <param name="handler">Обработчик</param>
        public void Subscription<T>(string queue, EventHandler<ConsumerReceivedEventArgs> handler)
        {
            Subscription? existSub = Subscriptions.FirstOrDefault(q
                => q.Id == SubscriptionIdGenerator.GenerateId<T>(queue));

            if (existSub != null)
            {
                existSub.ConsumerReceivedEvent += handler;
            }
            else
            {
                Subscriptions.Add(new Subscription(SubscriptionIdGenerator.GenerateId<T>(queue), handler));
            }
        }

        #endregion
    }
}