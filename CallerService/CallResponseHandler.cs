using Paramore.Brighter;
using Shared.Models;

namespace CallerService
{
    public class CallResponseHandler(ILogger<CallResponseHandler> loggger) : RequestHandlerAsync<CallResponse>
    {
        public override async Task<CallResponse> HandleAsync(CallResponse command, CancellationToken cancellationToken = default)
        {
            loggger.LogInformation($"Received response: '{command.Greeting}'");

            return await base.HandleAsync(command, cancellationToken);
        }
    }
}
