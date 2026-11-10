#if IOS || MACCATALYST

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using UIKit;

namespace TestApp.My.Mobile.Platforms.iOS;

/// <summary>
/// <see cref="ShellRenderer"/> that removes the iOS 15+ translucent "glass" tab bar effect so the
/// tab bar background colour set in the Shell is rendered flat.
/// </summary>
public sealed class PlainShellRenderer : ShellRenderer
{
    protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
    {
        return new PlainShellTabBarAppearanceTracker();
    }
}

public sealed class PlainShellTabBarAppearanceTracker : ShellTabBarAppearanceTracker
{
    public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
    {
        // Let MAUI do the normal Shell colour handling:
        // Shell.TabBarForegroundColor
        // Shell.TabBarTitleColor
        // Shell.TabBarUnselectedColor
        // Shell.TabBarDisabledColor
        // Shell.TabBarBackgroundColor
        base.SetAppearance(controller, appearance);

        RemoveGlassEffect(controller.TabBar, appearance);
    }

    public override void UpdateLayout(UITabBarController controller)
    {
        base.UpdateLayout(controller);

        RemoveGlassEffect(controller.TabBar, null);
        controller.TabBar.SetNeedsLayout();
    }

    private static void RemoveGlassEffect(UITabBar tabBar, ShellAppearance? appearance)
    {
        var backgroundColor = ToPlatformColor(
            appearance?.TabBarBackgroundColor,
            tabBar.BarTintColor ?? tabBar.BackgroundColor ?? UIColor.White);

        tabBar.Translucent = false;
        tabBar.Opaque = true;
        tabBar.BarTintColor = backgroundColor;
        tabBar.BackgroundColor = backgroundColor;
        tabBar.BackgroundImage = new UIImage();

        var standardAppearance = tabBar.StandardAppearance ?? new UITabBarAppearance();

        // Important: do NOT recreate item appearances here.
        // We only change the bar background/glass behaviour.
        standardAppearance.BackgroundEffect = null;
        standardAppearance.BackgroundColor = backgroundColor;
        standardAppearance.ShadowColor = UIColor.Clear;
        standardAppearance.ShadowImage = new UIImage();

        tabBar.StandardAppearance = standardAppearance;

        if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
        {
            var scrollEdgeAppearance = tabBar.ScrollEdgeAppearance ?? standardAppearance;

            scrollEdgeAppearance.BackgroundEffect = null;
            scrollEdgeAppearance.BackgroundColor = backgroundColor;
            scrollEdgeAppearance.ShadowColor = UIColor.Clear;
            scrollEdgeAppearance.ShadowImage = new UIImage();

            tabBar.ScrollEdgeAppearance = scrollEdgeAppearance;
        }
    }

    private static UIColor ToPlatformColor(Color? color, UIColor fallback)
    {
        if (color is null)
        {
            return fallback;
        }

        return UIColor.FromRGBA(
            (nfloat)color.Red,
            (nfloat)color.Green,
            (nfloat)color.Blue,
            (nfloat)color.Alpha);
    }
}

#endif
