namespace RabbitMQConnector.Enums
{
    /// <summary>
    /// Режим доставки
    /// </summary>
    public enum DeliveryMode : byte
    {
        /// <summary>
        /// Быстрый. Не сохраняет сообщение на диске
        /// </summary>
        Fast = 1,

        /// <summary>
        /// Гарантированный - чуть медленней. Сохраняет сообщения на диске
        /// </summary>
        Persistent = 2
    }
}