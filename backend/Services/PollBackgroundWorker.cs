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
        // Prevents overlapping poll-resolution cycles if a previous iteration
        // is still running when the next tick fires.
        private readonly SemaphoreSlim _cycleLock = new(1, 1);

        public PollBackgroundWorker(IServiceProvider serviceProvider, ILogger<PollBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (await _cycleLock.WaitAsync(0, stoppingToken))
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
                    finally
                    {
                        _cycleLock.Release();
                    }
                }
                else
                {
                    _logger.LogWarning("Poll background worker: previous cycle still running, skipping this tick.");
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

        public override void Dispose()
        {
            _cycleLock.Dispose();
            base.Dispose();
        }
    }
}
