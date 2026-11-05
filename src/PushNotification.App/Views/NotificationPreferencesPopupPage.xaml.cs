using PushNotification.App.ViewModels;

namespace PushNotification.App.Views;

/// <summary>
/// Modal popup that hosts the shared notification preference view. Shown after the user
/// allows the operating system notification permission.
/// </summary>
public partial class NotificationPreferencesPopupPage : ContentPage
{
    private readonly NotificationPreferencesView _preferencesView;

    public NotificationPreferencesPopupPage(NotificationPreferencesViewModel viewModel)
    {
        InitializeComponent();

        _preferencesView = new NotificationPreferencesView(viewModel) { ShowCloseButton = true };
        _preferencesView.CloseRequested += OnCloseRequested;
        Card.Content = _preferencesView;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _preferencesView.LoadAsync();
    }

    private async void OnCloseRequested(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();

    private async void OnScrimTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PopModalAsync();
}
