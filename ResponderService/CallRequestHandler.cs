using Paramore.Brighter;
using Shared.Models;

namespace ResponderService
{
    public class CallRequestHandler(ILogger<CallRequestHandler> logger, IAmACommandProcessor messageBus) : RequestHandlerAsync<CallRequest>
    {
        public override async Task<CallRequest> HandleAsync(CallRequest command, CancellationToken cancellationToken = default)
        {
            CallResponse response = new CallResponse()
            {
                SendersAddress = command.ReplyAddress,
                Greeting = $"Hello {command.Title} {command.Name}"
            };

            try
            {
                await messageBus.PostAsync(response, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send response");
            }

            return await base.HandleAsync(command, cancellationToken);
        }
    }
}
