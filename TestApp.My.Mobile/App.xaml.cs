using TestApp.My.Mobile.Helpers;
using TestApp.My.Mobile.Views;

namespace TestApp.My.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = ServiceHelper.GetService<AppShell>() ?? new AppShell();

            return new Window(shell)
            {
                Title = "TestApp.My.Mobile"
            };
        }
    }
}
