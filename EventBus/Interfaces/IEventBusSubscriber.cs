using System;
using EventBus.Events;

namespace EventBus.Interfaces
{
    /// <summary>
    /// Подписчик
    /// </summary>
    public interface IEventBusSubscriber
    {
        #region Публичные методы

        /// <summary>
        /// Подписка на получение сообщений
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <param name="consumer">Потребитель сообщений</param>
        void Subscribe<T>(string queue, EventHandler<ConsumerReceivedEventArgs> consumer);

        #endregion
    }
}