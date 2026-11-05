# PushNotification

A sample .NET 10 MAUI app that demonstrates push notifications on iOS and Android using
Firebase Cloud Messaging (FCM) and Apple Push Notification service (APNs), plus a
notification preference screen and a device installation registration flow.

## Contents

- [What the sample shows](#what-the-sample-shows)
- [Project layout](#project-layout)
- [Getting started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Android setup (FCM)](#android-setup-fcm)
  - [iOS setup (APNs)](#ios-setup-apns)
  - [Build and run](#build-and-run)
- [Configuration](#configuration)
- [How it works](#how-it-works)
- [Backend contract](#backend-contract)
- [Replacing the dummy services](#replacing-the-dummy-services)

For a full reference of every class, method and process, see
[docs/TECHNICAL-DOCUMENTATION.md](docs/TECHNICAL-DOCUMENTATION.md). For an exhaustive
method-level reference of every type, member, binding and process, see
[docs/COMPLETE-CODE-REFERENCE.md](docs/COMPLETE-CODE-REFERENCE.md).

## What the sample shows

- A guided permission flow started from the Home screen: an in-app soft prompt asks the user
  to allow notifications, the operating system dialog appears only after the user agrees, and
  the notification preference view opens as a popup once the permission is granted.
- FCM registration and message handling on Android, including the runtime
  `POST_NOTIFICATIONS` permission introduced in Android 13.
- APNs registration, foreground presentation and tap handling on iOS.
- A notification preference screen where a user opts in or out of individual topics. The same
  view is reused inside the post-permission popup.
- A device information screen that registers the device and saves the installation
  record (InstallationId, DeviceToken, Platform, Model and more) through dummy services.
- A clean service boundary so the dummy services can be swapped for real HTTP calls.

## Project layout

```
PushNotification.slnx
src/PushNotification.App/
  Configuration/            PushOptions bound from appsettings.json
  Models/                   Device, push message and preference models
  Services/
    Api/                    Device registration backend abstraction + dummy client
    Notifications/          IPushNotificationService and INotificationPermissionService
    Preferences/            Topic opt-in persistence and subscription sync
    DeviceInfoProvider.cs   Builds the installation snapshot
    PushRegistrationCoordinator.cs
    PushPermissionFlowService.cs  Soft prompt -> OS prompt -> preference popup
  ViewModels/               MVVM view models
  Views/                    Home, Notification preferences (shared view, tab host and popup),
                            Device info pages
  Platforms/
    Android/                FCM service, permission handling, message mapping
    iOS/                    APNs AppDelegate, permission handling
    Windows/ MacCatalyst/   Fallbacks so the shared UI runs everywhere
```

## Getting started

### Prerequisites

- .NET 10 SDK with the MAUI workload (`dotnet workload install maui`).
- Android: JDK 17, the Android SDK, and an emulator or device with Google Play services.
- iOS: macOS with Xcode 16 or later and a provisioning profile that enables push.

### Android setup (FCM)

1. Create a project in the [Firebase console](https://console.firebase.google.com/) and
   add an Android app with the package name `com.companyname.pushnotification.app`
   (change it in the `.csproj` if you use a different id).
2. Download `google-services.json`.
3. Save it to `src/PushNotification.App/Platforms/Android/google-services.json`.
   The `.csproj` picks the file up automatically when it exists. A reference copy lives
   in `google-services.json.template`.
4. Copy the project number into `Push.FcmSenderId` in `appsettings.json` if your code
   needs it at runtime.

`google-services.json` contains an API key. Do not commit the real file to source control.

### iOS setup (APNs)

1. In the Apple Developer portal, create an APNs key or certificate and enable the Push
   Notifications capability for your App ID.
2. Enable the Push Notifications capability for the app in Xcode or via a
   `Entitlements.plist` with `aps-environment` set to `development` or `production`.
3. Set `Push.ApnsTopic` in `appsettings.json` to your bundle identifier.
4. Push does not work on the iOS simulator; use a physical device.

### Build and run

```bash
# Android
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-android

# iOS (macOS only)
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-ios
```

On Linux the project targets `net10.0-android`. iOS, Mac Catalyst and Windows targets are
added when the build host supports them.

## Configuration

`appsettings.json` is embedded in the assembly and read at startup. Every value can be
overridden with an environment variable prefixed with `PUSH_`, for example
`PUSH_Push__FcmSenderId`.

```json
{
  "Push": {
    "ApiBaseUrl": "https://api.example.com",
    "FcmSenderId": "",
    "FcmNotificationChannelId": "general",
    "FcmNotificationChannelName": "General",
    "ApnsEnvironment": "development",
    "ApnsTopic": "com.companyname.pushnotification.app"
  }
}
```

## How it works

1. `MauiProgram` registers the shared services and, per platform, the push and permission
   implementations.
2. Tapping "Enable notifications" on the Home screen runs `PushPermissionFlowService`. It
   shows an in-app soft prompt first. Only when the user agrees does it call
   `INotificationPermissionService.RequestAsync` to show the operating system dialog. Once
   permission is granted it registers the device and opens the notification preference view
   as a popup.
3. `PushRegistrationCoordinator.RegisterAsync` reads the permission, initializes the push
   provider, reads the device token and calls the backend.
4. Android token refreshes arrive through `AppFirebaseMessagingService`, which forwards
   them to `FirebasePushBridge` and then to the singleton push service.
5. iOS device tokens and notification callbacks arrive in `AppDelegate`, which forwards
   them to `ApplePushBridge`.
6. `NotificationPreferencesService` stores each topic opt-in in `IPreferences`, updates
   the provider subscription and syncs the choice with the backend.

## Backend contract

`IDeviceRegistrationService` defines the calls the app makes. The sample ships with
`DummyDeviceRegistrationService`, which logs the payloads instead of sending them:

| Method | HTTP equivalent |
| --- | --- |
| `RegisterDeviceAsync` | `POST /api/devices/register` |
| `SaveInstallationAsync` | `PUT /api/installations/{installationId}` |
| `UnregisterDeviceAsync` | `DELETE /api/installations/{installationId}` |
| `SaveNotificationPreferencesAsync` | `PUT /api/installations/{installationId}/preferences` |

A registration payload looks like this:

```json
{
  "installationId": "6f1c...",
  "deviceToken": "fcm-or-apns-token",
  "platform": "Android",
  "userId": null,
  "topics": ["AccountAlerts", "OrderUpdates"],
  "appVersion": "1.0"
}
```

The installation record saved by `SaveInstallationAsync` includes InstallationId,
DeviceToken, Platform, PlatformName, Model, Manufacturer, DeviceName, OsVersion,
AppVersion, AppBuild, BundleId, Language, TimeZone, Locale, ScreenResolution,
ScreenDensity, DeviceType, IsEmulator and CapturedAtUtc.

## Replacing the dummy services

Replace `DummyDeviceRegistrationService` with an `HttpClient` based implementation and
register it in `MauiProgram.RegisterSharedServices`. The interface already models the
four operations the sample needs, so no view model changes are required.

This project is a sample. It was generated by an AI agent (OpenHands) on behalf of the
repository owner and is not production hardened.
