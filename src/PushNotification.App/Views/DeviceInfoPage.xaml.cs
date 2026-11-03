using PushNotification.App.ViewModels;

namespace PushNotification.App.Views;

public partial class DeviceInfoPage : ContentPage
{
    private readonly DeviceInfoViewModel _viewModel;

    public DeviceInfoPage(DeviceInfoViewModel viewModel)
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
