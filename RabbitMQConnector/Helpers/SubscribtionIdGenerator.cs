namespace RabbitMQConnector.Helpers
{
    /// <summary>
    /// Генератор Id для модели подписки
    /// </summary>
    public static class SubscriptionIdGenerator
    {
        #region Публичные методы

        /// <summary>
        /// Генерация идентификатора
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <returns></returns>
        public static string GenerateId<T>(string queue)
        {
            return GenerateId(queue, typeof(T).Name);
        }

        /// <summary>
        /// Генерация идентификатора
        /// </summary>
        /// <param name="queue">Очередь</param>
        /// <param name="type">Тип</param>
        /// <returns></returns>
        public static string GenerateId(string queue, string type)
        {
            return $"{queue}-{type}";
        }

        #endregion
    }
}