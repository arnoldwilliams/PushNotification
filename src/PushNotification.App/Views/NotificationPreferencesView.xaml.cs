using PushNotification.App.ViewModels;

namespace PushNotification.App.Views;

/// <summary>
/// The notification preference view. Shared by the Preferences tab page and the
/// preference popup that appears after the user allows push notifications.
/// </summary>
public partial class NotificationPreferencesView : ContentView
{
    private readonly NotificationPreferencesViewModel _viewModel;

    /// <summary>
    /// Creates the view. Instantiated from code so the shared view model can be injected;
    /// XAML cannot supply it because the container resolves the view model.
    /// </summary>
    public NotificationPreferencesView(NotificationPreferencesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>
    /// When <c>true</c> the header shows a Close button, used when the view is hosted in
    /// a popup. The tab page leaves it hidden and relies on tab navigation instead.
    /// </summary>
    public bool ShowCloseButton
    {
        get => CloseButton.IsVisible;
        set => CloseButton.IsVisible = value;
    }

    /// <summary>Raised when the user asks to close the view, for example from the popup.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Loads the current preferences into the view.</summary>
    public Task LoadAsync() => _viewModel.LoadAsync();

    private void OnCloseClicked(object? sender, EventArgs e) =>
        CloseRequested?.Invoke(this, EventArgs.Empty);
}
