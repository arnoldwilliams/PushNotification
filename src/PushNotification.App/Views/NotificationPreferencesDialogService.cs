using PushNotification.App.ViewModels;

namespace PushNotification.App.Views;

/// <summary>
/// Shows the notification preference view as a popup. Abstracted behind an interface so
/// the permission flow does not depend on the navigation stack directly.
/// </summary>
public interface INotificationPreferencesDialogService
{
    /// <summary>Presents the notification preferences popup and waits until it is dismissed.</summary>
    Task ShowAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class NotificationPreferencesDialogService : INotificationPreferencesDialogService
{
    private readonly NotificationPreferencesViewModel _viewModel;

    public NotificationPreferencesDialogService(NotificationPreferencesViewModel viewModel) =>
        _viewModel = viewModel;

    public async Task ShowAsync(CancellationToken cancellationToken = default)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
        {
            return;
        }

        var popup = new NotificationPreferencesPopupPage(_viewModel);
        await page.Navigation.PushModalAsync(popup).ConfigureAwait(true);
    }
}
