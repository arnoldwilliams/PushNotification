namespace PushNotification.App.Views;

/// <summary>
/// Customisable soft prompt shown before the operating system permission dialog. It is a
/// modal page with a Yes and a No button, so the wording, styling and actions can be
/// changed without touching the permission flow.
/// </summary>
public partial class NotificationSoftPromptPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Creates the prompt. The title and message default to the push permission wording and
    /// can be overridden per call.
    /// </summary>
    public NotificationSoftPromptPage(
        string title = "Notification Request",
        string message = "Allow Bansoft to send notification for updates",
        string acceptText = "Yes",
        string declineText = "No")
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        AcceptButton.Text = acceptText;
        DeclineButton.Text = declineText;
    }

    /// <summary>Completes when the user answers. True when the user tapped Yes.</summary>
    public Task<bool> Result => _completion.Task;

    /// <summary>The hardware/gesture back action counts as a No so the flow never hangs.</summary>
    protected override bool OnBackButtonPressed()
    {
        _completion.TrySetResult(false);
        return base.OnBackButtonPressed();
    }

    private void OnAcceptClicked(object? sender, EventArgs e) => _completion.TrySetResult(true);

    private void OnDeclineClicked(object? sender, EventArgs e) => _completion.TrySetResult(false);
}
