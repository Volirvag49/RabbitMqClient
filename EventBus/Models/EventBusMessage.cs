using System;
using System.Reflection;
using Newtonsoft.Json;

namespace EventBus.Models
{
    /// <summary>
    /// Сообщение
    /// </summary>
    public class EventBusMessage
    {
        #region Свойства

        /// <summary>
        /// Идентификатор сообщения
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Отправитель
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Тип рабочей нагрузки
        /// </summary>
        public string PayloadType { get; set; }

        /// <summary>
        /// Рабочая нагрузка
        /// </summary>
        public string Payload { get; set; }

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="payload">Рабочая нагрузка</param>
        public EventBusMessage(object payload)
        {
            Payload = payload != null
                ? JsonConvert.SerializeObject(payload)
                : throw new ArgumentNullException(nameof(payload));

            PayloadType = payload.GetType().Name;

            Id = Guid.NewGuid();
            CreatedDate = DateTime.Now;
            Sender = Assembly.GetEntryAssembly().GetName().Name;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public EventBusMessage()
        {
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Получить рабочую нагрузку
        /// </summary>
        /// <returns></returns>
        public T GetPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(Payload);
        }

        /// <summary>
        /// Получить рабочую нагрузку
        /// </summary>
        /// <returns></returns>
        public string GetPayloadJson()
        {
            return Payload;
        }

        #endregion
    }
}