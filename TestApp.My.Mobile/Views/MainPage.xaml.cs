using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.ViewModels;

namespace TestApp.My.Mobile.Views
{
    /// <summary>
    /// The registration work is triggered here, from <c>OnAppearing</c>, and executed by
    /// <see cref="PushNotificationViewModel"/>. This replaces the old
    /// <c>MainActivity.OnCreate</c> based registration.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly PushNotificationViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();

            _viewModel = ServiceHelper.GetService<PushNotificationViewModel>()
                ?? throw new InvalidOperationException(
                    $"{nameof(PushNotificationViewModel)} is not registered in the service container.");

            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await _viewModel.InitializeAsync();
        }
    }
}
