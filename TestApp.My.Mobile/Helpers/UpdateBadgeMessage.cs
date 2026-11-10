namespace TestApp.My.Mobile.Helpers
{
    /// <summary>
    /// Broadcast when the number of unread notifications changes so the iOS tab bar renderer
    /// (<c>BadgeShellRenderer</c>) can update the native badge. A value of zero clears the badge.
    /// </summary>
    public sealed class UpdateBadgeMessage
    {
        public UpdateBadgeMessage(int value)
        {
            Value = value;
        }

        /// <summary>Unread notification count. Zero hides the badge.</summary>
        public int Value { get; }
    }
}
