using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PollSystem.Services.Interfaces;

namespace PollSystem.Services
{
    public class PollBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PollBackgroundWorker> _logger;

        public PollBackgroundWorker(IServiceProvider serviceProvider, ILogger<PollBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var pollService = scope.ServiceProvider.GetRequiredService<IPollService>();

                    await pollService.EnsureDailyQuestionAsync();
                    await pollService.ResolveClosedPollsAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Poll background worker failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
