using System;
using RabbitMQ.Client;

namespace RabbitMQConnector.Connection
{
    /// <summary>
    /// Представляет логику подключения к шине
    /// </summary>
    public interface IPersistentConnection
    {
        #region События

        /// <summary>
        /// Событие, возникающее при попытке восстановления соединения после сбоя
        /// </summary>
        event EventHandler OnReconnectedAfterConnectionFailure;

        #endregion

        #region Свойства

        /// <summary>
        /// Есть активное подключение
        /// </summary>
        bool IsConnected { get; }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Попытка подключения
        /// </summary>
        /// <returns></returns>
        bool TryConnect();

        /// <summary>
        /// Создать AMQP-модель
        /// </summary>
        IModel CreateModel();

        #endregion
    }
}