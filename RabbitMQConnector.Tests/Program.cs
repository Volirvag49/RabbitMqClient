using Microsoft.Extensions.DependencyInjection;

namespace RabbitMQConnector.Tests
{
    /// <summary>
    /// Program
    /// </summary>
    internal class Program
    {
        #region Приватные методы

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            IServiceCollection services = Startup.ConfigureServices();
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<EntryPoint>().Run(args);
        }

        #endregion
    }
}