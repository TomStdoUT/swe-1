using PollSystem.Services.Interfaces;

namespace PollSystem.Services
{
    public class PollBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PollBackgroundWorker> _logger;
        private readonly SemaphoreSlim _cycleLock = new(1, 1);

        public PollBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<PollBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (await _cycleLock.WaitAsync(0))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
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
            base.Dispose();
            _cycleLock.Dispose();
        }
    }
}
