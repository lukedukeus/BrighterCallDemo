using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.RMQ.Async;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Paramore.Brighter.ServiceActivator.Extensions.Hosting;
using Shared;
using Shared.Models;

namespace ResponderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            var publications = GetPublications();
            var replySubscriptions = GetReplySubscriptions();
            var subscriptions = GetSubscriptions();

            var messageBus = builder.Services.AddBrighter().AutoFromAssemblies();

            messageBus.MapperRegistry(options =>
            {
                options.Register<CallRequest, ProtobufMessageMapper<CallRequest>>();
                options.RegisterAsync<CallRequest, ProtobufMessageMapper<CallRequest>>();

                options.Register<CallResponse, ProtobufMessageMapper<CallResponse>>();
                options.RegisterAsync<CallResponse, ProtobufMessageMapper<CallResponse>>();
            });

            string? connectionString = builder.Configuration.GetConnectionString("RabbitMQ");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException($"RabbitMQ connection string is empty or missing.");
            }

            if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var ampqUri))
            {
                throw new FormatException($"RabbitMQ  connection string was incorrectly formatted.");
            }

            var connection = new RmqMessagingGatewayConnection
            {
                AmpqUri = new AmqpUriSpecification(ampqUri),
                Exchange = new Exchange("messaging.exchange")
            };

            var producerFactory = new RmqProducerRegistryFactory(connection, publications);
            var consumerFactory = new RmqMessageConsumerFactory(connection);

            var producerRegistry = producerFactory.Create();

            messageBus.AddProducers(options =>
            {
                options.ProducerRegistry = producerRegistry;
                options.UseRpc = true;
                options.ReplyQueueSubscriptions = replySubscriptions;
                options.ResponseChannelFactory = new ChannelFactory(consumerFactory);
            });

            builder.Services.AddConsumers(options =>
            {
                options.Subscriptions = subscriptions;
                options.DefaultChannelFactory = new ChannelFactory(consumerFactory);
            });

            builder.Services.AddHostedService<ServiceActivatorHostedService>();

            var host = builder.Build();
            host.Run();
        }

        private static RmqPublication[] GetPublications()
        {
            return [
                new RmqPublication<CallResponse>
                {
                    MakeChannels = OnMissingChannel.Create,
                    Topic = new RoutingKey(nameof(CallResponse)),
                },
            ];
        }

        private static RmqSubscription[] GetSubscriptions()
        {
            return [
                new RmqSubscription<CallRequest>(
                    new SubscriptionName($"{nameof(CallRequest)}-{Guid.CreateVersion7()}"),
                    new ChannelName($"{nameof(CallRequest)}-{Guid.CreateVersion7()}"),
                    new RoutingKey(nameof(CallRequest)),
                    makeChannels: OnMissingChannel.Create
                ),
            ];
        }

        private static RmqSubscription[] GetReplySubscriptions()
        {
            return [];
        }
    }
}