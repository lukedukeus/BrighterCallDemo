using Paramore.Brighter;

namespace Shared
{
    public interface IReply
    {
        /// <summary>
        /// The channel that we should reply to the sender on.
        /// </summary>
        ReplyAddress SendersAddress { get; set; }
    }
}
