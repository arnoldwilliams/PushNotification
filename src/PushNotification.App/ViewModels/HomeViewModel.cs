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
    private readonly IPushPermissionFlowService _permissionFlow;
    private readonly ILogger<HomeViewModel> _logger;

    public HomeViewModel(
        IPushRegistrationCoordinator coordinator,
        IPushNotificationService pushNotificationService,
        INotificationPermissionService permissionService,
        IPushPermissionFlowService permissionFlow,
        ILogger<HomeViewModel> logger)
    {
        _coordinator = coordinator;
        _pushNotificationService = pushNotificationService;
        _permissionService = permissionService;
        _permissionFlow = permissionFlow;
        _logger = logger;

        _pushNotificationService.MessageReceived += OnMessageReceived;
        _pushNotificationService.NotificationTapped += OnNotificationTapped;

        RequestPermissionCommand = new Command(async () => await RequestPermissionAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    public ObservableCollection<PushMessage> ReceivedMessages { get; } = new();

    /// <summary>
    /// Starts the guided permission flow: in-app soft prompt, then the operating system
    /// dialog, then the notification preference popup once permission is granted.
    /// </summary>
    public ICommand RequestPermissionCommand { get; }

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

    private async Task RequestPermissionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Waiting for your choice...";

            var result = await _permissionFlow.RequestPermissionWithSoftPromptAsync().ConfigureAwait(true);

            StatusMessage = result.Outcome switch
            {
                PushPermissionOutcome.OsPermissionGranted =>
                    result.RegistrationResult?.Message ?? "Notifications enabled.",
                PushPermissionOutcome.OsPermissionDenied =>
                    "Notifications stay off until you allow them in system settings.",
                PushPermissionOutcome.SoftPromptDeclined =>
                    "No problem. You can turn notifications on later.",
                _ => "Could not request notification permission.",
            };

            await RefreshAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run the push permission flow.");
            StatusMessage = "Could not request notification permission.";
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
