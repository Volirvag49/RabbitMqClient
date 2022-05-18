using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQConnector.Extensions;

namespace RabbitMQConnector.Tests
{
    /// <summary>
    /// Startup
    /// </summary>
    public static class Startup
    {
        #region Публичные методы

        /// <summary>
        /// Конфигурирование сервисов
        /// </summary>
        public static IServiceCollection ConfigureServices()
        {
            ServiceCollection services = new ServiceCollection();
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, false);

            services.AddLogging(configure => configure.AddConsole());

            IConfiguration configuration = builder.Build();

            services.AddSingleton(configuration);
            services.AddSingleton<EntryPoint>();

            IConfigurationSection rabbitMqSection = configuration.GetSection("RabbitMQ");
            int.TryParse(rabbitMqSection["TimeoutBeforeReconnecting"], out int timeoutBeforeReconnecting);


            Console.WriteLine($"\n---Тестовое приложение конфигурирует шину событий для отправки сообщений");
            services.AddEventBusPublisher
            (
                rabbitMqSection["ConnectionUrl"],
                rabbitMqSection["ExchangeName"],
                timeoutBeforeReconnecting
            );

            Console.WriteLine($"\n---Тестовое приложение конфигурирует шину событий для получения сообщений\n");
            services.AddEventBusSubscriber
            (
                rabbitMqSection["ConnectionUrl"],
                rabbitMqSection["ExchangeName"],
                timeoutBeforeReconnecting
            );

            return services;
        }

        #endregion
    }
}