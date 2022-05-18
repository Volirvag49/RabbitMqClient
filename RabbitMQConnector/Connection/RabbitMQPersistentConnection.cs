using System;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQConnector.Connection
{
    /// <summary>
    /// Представляет логику подключения к шине RabbitMq
    /// </summary>
    public class RabbitMqPersistentConnection : IPersistentConnection, IDisposable
    {
        #region События

        /// <summary>
        /// Событие, возникающее при попытке восстановления соединения после сбоя
        /// </summary>
        public event EventHandler? OnReconnectedAfterConnectionFailure;

        #endregion

        #region Поля

        /// <summary>
        /// Фабрика подключений
        /// </summary>
        private readonly IConnectionFactory _connectionFactory;

        /// <summary>
        /// Тайм-аут передподключением
        /// </summary>
        private readonly TimeSpan _timeoutBeforeReconnecting;

        /// <summary>
        /// AMQP-Соединение
        /// </summary>
        private IConnection? _connection;

        /// <summary>
        /// Признак того, что неуправляемые ресурсы освобождены
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Объект блокировки потока
        /// </summary>
        private readonly object _locker = new object();

        /// <summary>
        /// ILogger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Ошибка подключения
        /// </summary>
        private bool _connectionFailed;

        #endregion

        #region Свойства

        /// <summary>
        /// Есть активное подключение
        /// </summary>
        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        #endregion

        #region Конструктор

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="connectionFactory">Фабрика подключений</param>
        /// <param name="logger">ILogger</param>
        /// <param name="timeoutBeforeReconnecting">Тайм-аут передподключением</param>
        public RabbitMqPersistentConnection
        (
            IConnectionFactory connectionFactory,
            ILogger logger,
            int timeoutBeforeReconnecting = 15
        )
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeoutBeforeReconnecting = TimeSpan.FromSeconds(timeoutBeforeReconnecting);
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Попытка подключения
        /// </summary>
        /// <returns></returns>
        public bool TryConnect()
        {
            _logger.LogInformation("Попытка подключения к RabbitMQ...");

            lock (_locker)
            {
                // Создает политику для повторных попыток подключения к брокеру сообщений до тех пор, пока это не удастся.
                RetryPolicy policy = Policy
                    .Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetryForever(duration => _timeoutBeforeReconnecting,
                        (ex, time) =>
                        {
                            _logger.LogWarning(ex,
                                "Клиент RabbitMQ не смог подключиться после таймаута: {TimeOut} сек. ({ExceptionMessage}). Ожидание повторной попытки...",
                                $"{(int) time.TotalSeconds}", ex.Message);
                        });

                policy.Execute(() => { _connection = _connectionFactory.CreateConnection(); });

                if (!IsConnected)
                {
                    _logger.LogCritical("Не удалось подключиться к RabbitMQ.");
                    _connectionFailed = true;
                    return false;
                }

                // Эти обработчики событий справляются с ситуациями,
                // пытаясь переподключить клиента,
                // когда соединение теряется по какой-либо причине.
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                _connection.ConnectionUnblocked += OnConnectionUnblocked;

                _logger.LogInformation(
                    "Клиент RabbitMQ установил постоянное соединение с хостом '{HostName}'.",
                    _connection.Endpoint.HostName);

                // Если соединение было прервано из-за отключения RabbitMQ или чего-то подобного, нужно гарантировать,
                // что обменник роуты и очереди будут существовать.
                // Также необходимо перепривязать все обработчики событий приложения.
                // Будем используем этот обработчик событий ниже, чтобы сделать это.
                if (_connectionFailed)
                {
                    OnReconnectedAfterConnectionFailure?.Invoke(this, null);
                    _connectionFailed = false;
                }

                return true;
            }
        }

        /// <summary>
        /// Создать AMQP-модель
        /// </summary>
        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                TryConnect();
            }

            return _connection.CreateModel();
        }

        /// <summary>
        /// Dispose - паттерн...
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _connection?.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Обработчик события при разрыве соединения RabbitMQ с ошибкой
        /// </summary>
        /// <param name="sender">Источник</param>
        /// <param name="args">аргументы</param>
        private void OnCallbackException(object sender, CallbackExceptionEventArgs args)
        {
            _connectionFailed = true;

            _logger.LogWarning("Ошибка подключения к RabbitMq. Повторная попытка переподключения...");
            TryConnectIfNotDisposed();
        }

        /// <summary>
        /// Обработчик события при отключении соединения RabbitMQ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnConnectionShutdown(object sender, ShutdownEventArgs args)
        {
            _connectionFailed = true;

            _logger.LogWarning("Отключение соединения RabbitMq. Попытка переподключения...");
            TryConnectIfNotDisposed();
        }

        /// <summary>
        /// Обработчик события при блокировке соединения RabbitMQ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs args)
        {
            _connectionFailed = true;

            _logger.LogWarning("Блокировка подключения RabbitMq. Повторная попытка переподключения...");
            TryConnectIfNotDisposed();
        }

        /// <summary>
        /// Обработчик события при разблокировке соединения RabbitMQ разблокировано
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnConnectionUnblocked(object sender, EventArgs args)
        {
            _connectionFailed = true;

            _logger.LogWarning("Разблокировка соединения RabbitMq. Попытка переподключения...");
            TryConnectIfNotDisposed();
        }

        /// <summary>
        /// Попытка соединения, при условии, что неуправляемые ресурсы используются
        /// </summary>
        private void TryConnectIfNotDisposed()
        {
            if (_disposed)
            {
                _logger.LogInformation("Клиент RabbitMQ отключился. Больше никаких действий предприниматься не будет.");
                return;
            }

            TryConnect();
        }

        #endregion
    }
}