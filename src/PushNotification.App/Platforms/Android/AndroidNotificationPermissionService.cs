using Android;
using Android.App;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using PushNotification.App.Models;

namespace PushNotification.App.Services.Notifications;

/// <summary>
/// Android notification permission handling. From API 33 the app must request the
/// runtime <c>POST_NOTIFICATIONS</c> permission; older versions are granted implicitly.
/// </summary>
public sealed class AndroidNotificationPermissionService : INotificationPermissionService
{
    private const int RequestCode = 0x5100;

    private static TaskCompletionSource<NotificationPermissionStatus>? _pendingRequest;

    public Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return Task.FromResult(NotificationPermissionStatus.Granted);
        }

        var context = Android.App.Application.Context;
        var granted = ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications)
            == Permission.Granted;

        return Task.FromResult(granted
            ? NotificationPermissionStatus.Granted
            : NotificationPermissionStatus.Denied);
    }

    public Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return Task.FromResult(NotificationPermissionStatus.Granted);
        }

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            return Task.FromResult(NotificationPermissionStatus.Unknown);
        }

        _pendingRequest = new TaskCompletionSource<NotificationPermissionStatus>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ActivityCompat.RequestPermissions(
            activity,
            new[] { Manifest.Permission.PostNotifications },
            RequestCode);

        return _pendingRequest.Task;
    }

    /// <summary>Called by <see cref="MainActivity"/> when the permission dialog closes.</summary>
    internal static void HandlePermissionResult(string[] permissions, Permission[] grantResults)
    {
        var pending = Interlocked.Exchange(ref _pendingRequest, null);
        if (pending is null)
        {
            return;
        }

        var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
        pending.TrySetResult(granted
            ? NotificationPermissionStatus.Granted
            : NotificationPermissionStatus.Denied);
    }
}
