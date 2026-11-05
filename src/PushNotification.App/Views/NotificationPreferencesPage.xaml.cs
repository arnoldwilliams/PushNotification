using PushNotification.App.ViewModels;

namespace PushNotification.App.Views;

/// <summary>
/// Preferences tab. Hosts the shared <see cref="NotificationPreferencesView"/> so the tab
/// and the post-permission popup render exactly the same content.
/// </summary>
public partial class NotificationPreferencesPage : ContentPage
{
    private readonly NotificationPreferencesView _preferencesView;

    public NotificationPreferencesPage(NotificationPreferencesViewModel viewModel)
    {
        InitializeComponent();

        _preferencesView = new NotificationPreferencesView(viewModel);
        Host.Content = _preferencesView;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _preferencesView.LoadAsync();
    }
}
