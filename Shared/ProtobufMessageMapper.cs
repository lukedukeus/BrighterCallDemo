using Google.Protobuf;
using Paramore.Brighter;
using Paramore.Brighter.Extensions;
using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace Shared
{
    public class ProtobufMessageMapper<T> : IAmAMessageMapper<T>, IAmAMessageMapperAsync<T> where T : class, IRequest, IMessage<T>, new()
    {
        private readonly MessageParser<T> parser = new MessageParser<T>(() => new T());
        private readonly ContentType contentType = new ContentType("application/protobuf");
        public IRequestContext Context { get; set; }

        public Message MapToMessage(T request, Publication publication)
        {
            Id? correlationId = null;
            RoutingKey? replyTo = null;
            RoutingKey? topic = publication.Topic;

            if (request is ICall call)
            {
                correlationId = call.ReplyAddress.CorrelationId;
                replyTo = call.ReplyAddress.Topic;
            }
            else if (request is IReply reply)
            {
                correlationId = reply.SendersAddress.CorrelationId;
                topic = reply.SendersAddress.Topic;
            }

            if (topic is null)
            {
                throw new InvalidOperationException("Unable to determine topic for message");
            }

            var header = new MessageHeader(
                messageId: request.Id,
                topic: topic,
                messageType: request.RequestToMessageType(),
                correlationId: correlationId,
                replyTo: replyTo
            );

            var payload = request.ToByteArray();
            var body = new MessageBody(payload, contentType, CharacterEncoding.Raw);

            return new Message(header, body);
        }

        public T MapToRequest(Message message)
        {
            var request = parser.ParseFrom(message.Body.Bytes);

            if (request is ICall call)
            {
                if (message.Header.ReplyTo is not null)
                {
                    call.ReplyAddress.Topic = message.Header.ReplyTo;
                }

                call.ReplyAddress.CorrelationId = message.Header.CorrelationId;
            }
            else if (request is IReply reply)
            {
                reply.SendersAddress.Topic = message.Header.Topic;
                reply.SendersAddress.CorrelationId = message.Header.CorrelationId;
            }

            return request;
        }

        public Task<Message> MapToMessageAsync(T request, Publication publication, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MapToMessage(request, publication));
        }

        public Task<T> MapToRequestAsync(Message message, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MapToRequest(message));
        }
    }
}
