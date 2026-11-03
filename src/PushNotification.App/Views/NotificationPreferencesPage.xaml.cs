using PushNotification.App.ViewModels;

namespace PushNotification.App.Views;

public partial class NotificationPreferencesPage : ContentPage
{
    private readonly NotificationPreferencesViewModel _viewModel;

    public NotificationPreferencesPage(NotificationPreferencesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
