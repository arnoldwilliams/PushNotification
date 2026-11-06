using Microsoft.Extensions.Logging;
using PushNotification.App.Models;
using PushNotification.App.Services.Notifications;
using PushNotification.App.Views;

namespace PushNotification.App.Services;

/// <summary>
/// Result of the guided push permission flow.
/// </summary>
/// <param name="Outcome">How the flow finished.</param>
/// <param name="PermissionStatus">Permission state reported by the platform after the flow.</param>
/// <param name="RegistrationResult">Registration result when the device was registered.</param>
public sealed record PushPermissionFlowResult(
    PushPermissionOutcome Outcome,
    NotificationPermissionStatus PermissionStatus,
    DeviceRegistrationResult? RegistrationResult)
{
    /// <summary>True when the user allowed the operating system permission.</summary>
    public bool IsGranted => Outcome == PushPermissionOutcome.OsPermissionGranted;
}

/// <summary>
/// Drives the guided permission experience: show an in-app soft prompt, then the
/// operating system permission dialog when the user agrees, and finally present the
/// notification preference view as a popup when the permission is granted.
/// </summary>
public interface IPushPermissionFlowService
{
    /// <summary>
    /// Runs the flow on the currently visible page. When the permission is granted the
    /// notification preference popup is shown unless <paramref name="showPreferencesOnGrant"/> is false.
    /// </summary>
    Task<PushPermissionFlowResult> RequestPermissionWithSoftPromptAsync(
        bool showPreferencesOnGrant = true,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class PushPermissionFlowService : IPushPermissionFlowService
{
    private readonly INotificationPermissionService _permissionService;
    private readonly IPushRegistrationCoordinator _coordinator;
    private readonly INotificationPreferencesDialogService _preferencesDialog;
    private readonly ILogger<PushPermissionFlowService> _logger;

    public PushPermissionFlowService(
        INotificationPermissionService permissionService,
        IPushRegistrationCoordinator coordinator,
        INotificationPreferencesDialogService preferencesDialog,
        ILogger<PushPermissionFlowService> logger)
    {
        _permissionService = permissionService;
        _coordinator = coordinator;
        _preferencesDialog = preferencesDialog;
        _logger = logger;
    }

    public async Task<PushPermissionFlowResult> RequestPermissionWithSoftPromptAsync(
        bool showPreferencesOnGrant = true,
        CancellationToken cancellationToken = default)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
        {
            _logger.LogWarning("No visible page available to host the soft prompt.");
            return new PushPermissionFlowResult(PushPermissionOutcome.Unavailable, NotificationPermissionStatus.Unknown, null);
        }

        // 1. In-app soft prompt. Nothing is asked of the operating system yet.
        var agrees = await ShowSoftPromptAsync(page).ConfigureAwait(true);
        if (!agrees)
        {
            _logger.LogInformation("The user declined or dismissed the soft prompt.");
            return new PushPermissionFlowResult(PushPermissionOutcome.SoftPromptDeclined, NotificationPermissionStatus.NotRequested, null);
        }

        // 2. The user agreed, so the operating system dialog may be shown.
        var status = await _permissionService.RequestAsync(cancellationToken).ConfigureAwait(true);
        if (status != NotificationPermissionStatus.Granted)
        {
            _logger.LogInformation("Operating system permission is {Status}.", status);
            return new PushPermissionFlowResult(PushPermissionOutcome.OsPermissionDenied, status, null);
        }

        // 3. Permission granted: register the device and show the preference popup.
        var registration = await _coordinator.RegisterAsync(requestPermission: false, cancellationToken).ConfigureAwait(true);

        if (showPreferencesOnGrant)
        {
            await _preferencesDialog.ShowAsync().ConfigureAwait(true);
        }

        return new PushPermissionFlowResult(PushPermissionOutcome.OsPermissionGranted, status, registration);
    }

    /// <summary>
    /// Shows the customisable soft prompt page modally and waits for the user's answer.
    /// Returns true only when the user taps Yes. Dismissing the page (scrim tap) is a No.
    /// </summary>
    private static async Task<bool> ShowSoftPromptAsync(Page page)
    {
        var prompt = new NotificationSoftPromptPage();
        await MainThread.InvokeOnMainThreadAsync(() => page.Navigation.PushModalAsync(prompt)).ConfigureAwait(true);

        var agrees = await prompt.Result.ConfigureAwait(true);

        await MainThread.InvokeOnMainThreadAsync(() => page.Navigation.PopModalAsync()).ConfigureAwait(true);
        return agrees;
    }
}
