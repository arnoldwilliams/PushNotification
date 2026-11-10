using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using TestApp.My.Mobile.Helpers;
using UIKit;

namespace TestApp.My.Mobile.Platforms.iOS
{
    /// <summary>
    /// <see cref="ShellRenderer"/> that can show a native tab bar badge.
    /// <para>
    /// The badge is driven by <see cref="UpdateBadgeMessage"/>, which the view model publishes when
    /// a push notification arrives (and when the unread count is cleared).
    /// </para>
    /// </summary>
    public class BadgeShellRenderer : ShellRenderer
    {
        protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
            => new BadgeShellTabBarAppearanceTracker();
    }

    public class BadgeShellTabBarAppearanceTracker : ShellTabBarAppearanceTracker
    {
        // Zero based index of the tab that shows the badge. The sample app has a single tab.
        private const int TargetTabIndex = 0;

        private UITabBarController? _tabBarController;

        public BadgeShellTabBarAppearanceTracker()
        {
            WeakReferenceMessenger.Default.Register<UpdateBadgeMessage>(this, (_, message) =>
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateNativeBadge(message.Value));
            });
        }

        public override void UpdateLayout(UITabBarController controller)
        {
            base.UpdateLayout(controller);

            _tabBarController = controller;
        }

        private void UpdateNativeBadge(int count)
        {
            if (_tabBarController?.TabBar?.Items == null)
            {
                return;
            }

            try
            {
                if (_tabBarController.TabBar.Items.Length <= TargetTabIndex)
                {
                    return;
                }

                var tabBarItem = _tabBarController.TabBar.Items[TargetTabIndex];

                if (tabBarItem == null)
                {
                    return;
                }

                if (count > 0)
                {
                    tabBarItem.BadgeValue = count.ToString();
                    tabBarItem.BadgeColor = UIColor.Red;
                }
                else
                {
                    tabBarItem.BadgeValue = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[iOS Badge Error] {ex.Message}");
            }
        }
    }
}
