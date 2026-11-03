using PushNotification.App.Models;

namespace PushNotification.App.ViewModels;

/// <summary>
/// One row in the notification preference list. Wraps a
/// <see cref="NotificationPreference"/> and notifies the view model when toggled.
/// </summary>
public sealed class NotificationPreferenceItem : BaseViewModel
{
    private readonly Action<NotificationPreferenceItem> _onToggled;
    private bool _isEnabled;

    public NotificationPreferenceItem(
        NotificationPreference preference,
        Action<NotificationPreferenceItem> onToggled)
    {
        Topic = preference.Topic;
        DisplayName = preference.DisplayName;
        Description = preference.Description;
        _isEnabled = preference.IsEnabled;
        _onToggled = onToggled;
    }

    public NotificationTopic Topic { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                _onToggled(this);
            }
        }
    }

    /// <summary>Updates the backing field without raising the toggle callback.</summary>
    public void SetEnabledSilently(bool enabled) => SetProperty(ref _isEnabled, enabled, nameof(IsEnabled));
}
