using System;
using EventBus.Models;
using Newtonsoft.Json;

namespace EventBus.Events
{
    /// <summary>
    /// Аргументы события
    /// </summary>
    public class ConsumerReceivedEventArgs : EventArgs
    {
        #region Свойства

        /// <summary>
        /// Сообщение
        /// </summary>
        public EventBusMessage Message { get; set; }

        /// <summary>
        /// Маркер того, что сообщение обработано успешно
        /// и его не требуется возвращать в очередь
        /// </summary>
        public bool IsReceived { get; private set; }

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="message">Сообщение</param>
        public ConsumerReceivedEventArgs(EventBusMessage message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Получить сообщение в виде json
        /// </summary>
        /// <returns></returns>
        public string GetMessageJson()
        {
            return JsonConvert.SerializeObject(Message);
        }

        /// <summary>
        /// Получить рабочую нагрузку
        /// </summary>
        /// <returns></returns>
        public T GetPayload<T>()
        {
            return Message.GetPayload<T>();
        }

        /// <summary>
        /// Получить рабочую нагрузку
        /// </summary>
        /// <returns></returns>
        public string GetPayloadJson()
        {
            return Message.GetPayloadJson();
        }

        /// <summary>
        /// Сообщение обработано
        /// </summary>
        public void Received()
        {
            IsReceived = true;
        }

        #endregion
    }
}