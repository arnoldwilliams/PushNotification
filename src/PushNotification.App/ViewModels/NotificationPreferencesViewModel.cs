using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PushNotification.App.Models;
using PushNotification.App.Services.Api;
using PushNotification.App.Services.Preferences;

namespace PushNotification.App.ViewModels;

/// <summary>
/// Backs the notification preference view. Each row toggles a topic on/off; a master
/// switch enables or disables every topic at once.
/// </summary>
public sealed class NotificationPreferencesViewModel : BaseViewModel
{
    private readonly INotificationPreferencesService _preferencesService;
    private readonly IDeviceRegistrationService _registrationService;
    private readonly ILogger<NotificationPreferencesViewModel> _logger;

    public NotificationPreferencesViewModel(
        INotificationPreferencesService preferencesService,
        IDeviceRegistrationService registrationService,
        ILogger<NotificationPreferencesViewModel> logger)
    {
        _preferencesService = preferencesService;
        _registrationService = registrationService;
        _logger = logger;

        ToggleAllCommand = new Command<bool>(async enabled => await ToggleAllAsync(enabled));
    }

    /// <summary>Topic rows rendered by the view.</summary>
    public ObservableCollection<NotificationPreferenceItem> Preferences { get; } = new();

    public ICommand ToggleAllCommand { get; }

    private bool _allEnabled;

    /// <summary>Master switch state, kept in sync with the individual toggles.</summary>
    public bool AllEnabled
    {
        get => _allEnabled;
        set
        {
            if (SetProperty(ref _allEnabled, value))
            {
                _ = ToggleAllAsync(value);
            }
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var stored = await _preferencesService.GetPreferencesAsync(cancellationToken).ConfigureAwait(true);

            Preferences.Clear();
            foreach (var preference in stored)
            {
                Preferences.Add(new NotificationPreferenceItem(preference, OnItemToggled));
            }

            _allEnabled = Preferences.Count > 0 && Preferences.All(p => p.IsEnabled);
            OnPropertyChanged(nameof(AllEnabled));
            StatusMessage = $"{Preferences.Count(p => p.IsEnabled)} of {Preferences.Count} topics enabled.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleAllAsync(bool enabled)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var updated = await _preferencesService.SetAllAsync(enabled).ConfigureAwait(true);

            foreach (var item in Preferences)
            {
                item.SetEnabledSilently(updated.First(p => p.Topic == item.Topic).IsEnabled);
            }

            StatusMessage = enabled ? "All notifications enabled." : "All notifications disabled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle all notification preferences.");
            StatusMessage = "Could not update preferences.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnItemToggled(NotificationPreferenceItem item)
    {
        try
        {
            IsBusy = true;

            await _preferencesService.SetEnabledAsync(item.Topic, item.IsEnabled).ConfigureAwait(true);

            _allEnabled = Preferences.All(p => p.IsEnabled);
            OnPropertyChanged(nameof(AllEnabled));
            StatusMessage = $"{item.DisplayName} {(item.IsEnabled ? "enabled" : "disabled")}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preference for {Topic}.", item.Topic);
            item.SetEnabledSilently(!item.IsEnabled);
            StatusMessage = "Could not update the preference.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
