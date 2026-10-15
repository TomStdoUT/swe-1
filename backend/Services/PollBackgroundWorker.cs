using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PollSystem.Services.Interfaces;

namespace PollSystem.Services
{
    public class PollBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public PollBackgroundWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var pollService = scope.ServiceProvider.GetRequiredService<IPollService>();
                    
                    await pollService.EnsureDailyQuestionAsync();
                    await pollService.ResolveClosedPollsAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}