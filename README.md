# TestApp.My.Mobile — .NET 10 MAUI Push Notifications (Android + iOS)

A sample .NET 10 MAUI app that registers a device for push notifications on Android (Firebase
Cloud Messaging v1) and iOS (APNs).

The important change over the original sample: the device registration no longer runs in
`MainActivity.OnCreate`. It now runs in `PushNotificationViewModel.InitializeAsync`, which is
triggered from `MainPage.OnAppearing`. `MainActivity.OnCreate` only does platform plumbing
(notification channel, message bus, deep links).

Full design and setup details are in [`docs/push-notification-registration.md`](docs/push-notification-registration.md).

## Layout

| Path | Contents |
| --- | --- |
| `TestApp.My.Mobile/ViewModels/PushNotificationViewModel.cs` | The registration lifecycle (permission, token, register, deregister, refresh) |
| `TestApp.My.Mobile/LocalServices/Services/NotificationRegistrationService.cs` | Backend registration calls (platform agnostic) |
| `TestApp.My.Mobile/Platforms/Android/` | `AndroidDeviceInstallationService`, `AndroidPushNotificationService`, `MainActivity`, `TestAppFirebaseMessagingService`, `PNSToken*Listener` |
| `TestApp.My.Mobile/Platforms/iOS/` | `iOSDeviceInstallationService`, `iOSPushNotificationService`, `AppDelegate` |
| `docs/` | Technical documentation |

The original reference files are kept in the repository root
(`AndroidDeviceInstallationService.cs`, `MainActivity.cs`, `NotificationRegistrationService.cs`,
`PNSToken*Listener.cs`, `TestAppFirebaseMessagingService.cs`) for comparison. The project itself
does not compile them; the equivalents live under `TestApp.My.Mobile/`.

## Prerequisites

- .NET 10 SDK
- MAUI workloads: `dotnet workload install maui-android maui-ios`
- JDK 17+ and the Android SDK (API 36) for Android
- Xcode 16+ on macOS for iOS

## Build

```bash
# Android
dotnet build TestApp.My.Mobile/TestApp.My.Mobile.csproj -f net10.0-android

# iOS (macOS only)
dotnet build TestApp.My.Mobile/TestApp.My.Mobile.csproj -f net10.0-ios
```

## Configure

- **Android**: create a Firebase app with package name `com.testapp.my.mobile`, then place
  `google-services.json` in `TestApp.My.Mobile/Platforms/Android/`
  (see `google-services.json.template`).
- **iOS**: enable the Push Notifications capability and set `aps-environment` in
  `Platforms/iOS/Entitlements.plist`.
- **Backend**: set `BasePushNotificationsUrl` in
  `TestApp.My.Mobile/LocalServices/Services/ConfigurationService.cs`.
