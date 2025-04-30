using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ai_agents_hack_tariffed.ApiService
{
    public class ConsumeScopedServiceHostedService : BackgroundService
    {
        private readonly ILogger<ConsumeScopedServiceHostedService> _logger;

        public ConsumeScopedServiceHostedService(IServiceProvider services,
            ILogger<ConsumeScopedServiceHostedService> logger)
        {
            Services = services;
            _logger = logger;
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service running.");

            await DoWork(stoppingToken);
        }

        /// <summary>
        /// Executes the background work using a scoped service.
        /// </summary>
        /// <remarks>This method creates a new service scope to resolve and execute the scoped processing
        /// service.  If the scoped service completes its work successfully, the background service is
        /// stopped.</remarks>
        /// <param name="stoppingToken">A cancellationToken that is monitored for cancellation requests.</param>
        /// <returns></returns>
        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IScopedProcessingService>();

                var result = await scopedProcessingService.DoWork(stoppingToken);
                if (result)
                {
                    await StopAsync(stoppingToken);
                    _logger.LogInformation("✅ Scoped background service stopped.");
                }
            }
        }

        /// <summary>
        /// Stops the hosted service and performs any necessary cleanup operations.
        /// </summary>
        /// <param name="stoppingToken">Can be used to signal the stop request or observe cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}