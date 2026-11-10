using CommunityToolkit.Mvvm.Messaging;

namespace TestApp.My.Mobile.Common
{
    /// <summary>
    /// Thin, string channel based wrapper over <see cref="WeakReferenceMessenger"/>.
    /// </summary>
    public sealed class MessageBus
    {
        public static MessageBus Instance { get; } = new();

        private MessageBus()
        {
        }

        public void Subscribe<T>(string channel, Action<T> handler)
        {
            WeakReferenceMessenger.Default.Register<ChannelMessage<T>, string>(
                this,
                channel,
                (_, message) => handler(message.Value));
        }

        public void Unsubscribe<T>(string channel)
            => WeakReferenceMessenger.Default.Unregister<ChannelMessage<T>, string>(this, channel);

        public void Publish<T>(string channel, T value)
            => WeakReferenceMessenger.Default.Send(new ChannelMessage<T>(channel, value));
    }
}
