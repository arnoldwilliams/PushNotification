using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PushNotification.App.Models;
using PushNotification.App.Services;
using PushNotification.App.Services.Notifications;

namespace PushNotification.App.ViewModels;

/// <summary>
/// Home screen. Summarizes push state and shows a live log of messages received
/// while the app is running.
/// </summary>
public sealed class HomeViewModel : BaseViewModel
{
    private readonly IPushRegistrationCoordinator _coordinator;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly INotificationPermissionService _permissionService;
    private readonly ILogger<HomeViewModel> _logger;

    public HomeViewModel(
        IPushRegistrationCoordinator coordinator,
        IPushNotificationService pushNotificationService,
        INotificationPermissionService permissionService,
        ILogger<HomeViewModel> logger)
    {
        _coordinator = coordinator;
        _pushNotificationService = pushNotificationService;
        _permissionService = permissionService;
        _logger = logger;

        _pushNotificationService.MessageReceived += OnMessageReceived;
        _pushNotificationService.NotificationTapped += OnNotificationTapped;

        EnablePushCommand = new Command(async () => await EnablePushAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    public ObservableCollection<PushMessage> ReceivedMessages { get; } = new();

    public ICommand EnablePushCommand { get; }

    public ICommand RefreshCommand { get; }

    private string _permissionStatus = "Unknown";

    public string PermissionStatus
    {
        get => _permissionStatus;
        private set => SetProperty(ref _permissionStatus, value);
    }

    private string _deviceToken = "Not available";

    public string DeviceToken
    {
        get => _deviceToken;
        private set => SetProperty(ref _deviceToken, value);
    }

    public async Task LoadAsync()
    {
        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task EnablePushAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Requesting permission and registering...";

            var result = await _coordinator.RegisterAsync().ConfigureAwait(true);
            StatusMessage = result.Success ? result.Message ?? "Registered." : result.Message ?? "Not registered.";

            await RefreshAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable push notifications.");
            StatusMessage = "Could not enable push notifications.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            var status = await _permissionService.GetStatusAsync().ConfigureAwait(true);
            PermissionStatus = status.ToString();

            var token = _coordinator.CurrentDeviceToken
                ?? await _pushNotificationService.GetDeviceTokenAsync().ConfigureAwait(true);
            DeviceToken = string.IsNullOrWhiteSpace(token) ? "Not available" : token;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not refresh push state.");
        }
    }

    private void OnMessageReceived(object? sender, PushMessage message) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReceivedMessages.Insert(0, message);
            StatusMessage = $"Received: {message.Title ?? message.Body ?? message.MessageId}";
        });

    private void OnNotificationTapped(object? sender, PushMessage message) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReceivedMessages.Insert(0, message);
            StatusMessage = $"Opened from notification: {message.Title ?? message.Body}";
        });
}
