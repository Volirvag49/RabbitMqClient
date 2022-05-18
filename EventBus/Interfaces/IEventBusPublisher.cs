using EventBus.Models;

namespace EventBus.Interfaces
{
    /// <summary>
    /// Отправитель
    /// </summary>
    public interface IEventBusPublisher
    {
        #region Публичные методы

        /// <summary>
        /// Публикация сообщения
        /// </summary>
        /// <param name="route">Роут</param>
        /// <param name="message">Сообщение</param>
        void Publish(string route, EventBusMessage message);

        #endregion
    }
}