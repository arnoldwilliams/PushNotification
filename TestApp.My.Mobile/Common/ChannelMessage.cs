namespace TestApp.My.Mobile.Common
{
    /// <summary>
    /// A typed message that carries a channel (topic) so subscribers only receive what they asked for.
    /// </summary>
    public sealed record ChannelMessage<T>(string Channel, T Value);
}
