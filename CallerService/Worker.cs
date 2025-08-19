using Paramore.Brighter;
using Shared.Models;

namespace CallerService
{
    public class Worker(ILogger<Worker> logger, IAmACommandProcessor messageBus) : BackgroundService
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(5);
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Calling ResponderService...");

                var response = messageBus.Call<CallRequest, CallResponse>(new CallRequest
                {
                    Name = "Jones",
                    Title = "Mr."
                }, timeOut: timeout);

                if (response is not null)
                {
                    logger.LogInformation("Received response: '{Response}'", response.Greeting);
                }
                else
                {
                    logger.LogWarning("No response received.");
                }

                logger.LogInformation("Making next call in 5s...");

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
