# Push Notification Device Registration in a .NET 10 MAUI App (Android + iOS)

Technical design document for the `TestApp.My.Mobile` sample.

## 1. Purpose

This document explains a .NET 10 MAUI application that registers a device for push
notifications on Android (Firebase Cloud Messaging v1 / FCMv1) and iOS (Apple Push
Notification service / APNs).

The original implementation performed the device installation inside
`MainActivity.OnCreate`:

```csharp
if (DeviceInstallationService.NotificationsSupported)
{
    FirebaseMessaging.Instance.GetToken()
        .AddOnSuccessListener(new PNSTokenSuccessListener(token =>
        {
            DeviceInstallationService.Token = token;

            NotificationRegistrationService.RefreshRegistrationAsync(token)
                .ContinueWith(task => { if (task.IsFaulted) throw task.Exception; });
        }));
}
```

This sample moves that responsibility into a **view model** (`PushNotificationViewModel`).
`OnCreate` now only does platform plumbing (notification channel, deep link handling).

## 2. Goals

| Goal | How it is achieved |
| --- | --- |
| Registration happens in a view model, not `OnCreate` | `PushNotificationViewModel.InitializeAsync` is invoked from `MainPage.OnAppearing` |
| One code path for Android and iOS | Shared `PushNotificationViewModel` talks to abstractions only |
| Testable, mock-free core | Interfaces for device installation, registration and the platform push service |
| Keep the original file names and responsibilities | `AndroidDeviceInstallationService`, `MainActivity`, `NotificationRegistrationService`, `PNSToken*Listener`, `TestAppFirebaseMessagingService` are preserved |
| Full lifecycle coverage | Permission, first token, token refresh, foreground message, notification tap, deregistration |

## 3. Solution layout

```
PushNotification/
├─ TestApp.My.Mobile.sln
├─ TestApp.My.Mobile/
│  ├─ TestApp.My.Mobile.csproj          # net10.0-android; net10.0-ios
│  ├─ MauiProgram.cs                    # DI + platform registration
│  ├─ App.xaml(.cs) / AppShell.xaml(.cs)
│  ├─ Common/                           # MessageBus, ChannelMessage, InvertedBoolConverter
│  ├─ Helpers/                          # Constants, ServiceHelper
│  ├─ Dto/Notifications/…               # DTOs (namespace MyTestApp.Dto.Notifications[.Service])
│  ├─ Interfaces/                       # IHttpCommunication, IPushNotificationService,
│  │                                    # IPushNotificationEventSink
│  ├─ LocalServices/
│  │  ├─ Interfaces/                    # IDeviceInstallationService, INotificationRegistrationService,
│  │  │                                 # ISecureStorageService, IConfigurationService
│  │  └─ Services/                      # NotificationRegistrationService, SecureStorageService,
│  │                                    # ConfigurationService
│  ├─ Services/                         # HttpCommunication, LoggingService
│  ├─ ViewModels/PushNotificationViewModel.cs
│  ├─ Views/MainPage.xaml(.cs)
│  └─ Platforms/
│     ├─ Android/                       # AndroidDeviceInstallationService, AndroidPushNotificationService,
│     │                                 # MainActivity, MainApplication, TestAppFirebaseMessagingService,
│     │                                 # PNSTokenSuccessListener / PNSTokenFailureListener /
│     │                                 # PNSTokenCompleteListener, AndroidManifest.xml
│     └─ iOS/                           # iOSDeviceInstallationService, iOSPushNotificationService,
│                                       # AppDelegate, Info.plist, Entitlements.plist
└─ docs/                                # this document
```

Namespaces match the original code:

- `TestApp.My.Mobile.Platforms.Android`
- `TestApp.My.Mobile.Platforms.iOS`
- `TestApp.My.Mobile.LocalServices.Services`
- `MyTestApp.Dto.Notifications.Service`

## 4. Architecture

```
        ┌──────────────────────────┐
        │  MainPage (View)         │  OnAppearing → InitializeAsync()
        └────────────┬─────────────┘
                     │ binds / commands
        ┌────────────▼─────────────┐
        │ PushNotificationViewModel│  ← all registration logic lives here
        └───┬───────────┬──────────┘
            │           │
   IPushNotification   INotificationRegistrationService   IDeviceInstallationService
   Service (platform)  (backend HTTP)                     (device payload)
            │           │                                  │
   ┌────────▼──────┐  ┌─▼────────────────────────┐  ┌──────▼─────────────────┐
   │ Android / iOS │  │ NotificationRegistration │  │ Android / iOS device   │
   │ push service  │  │ Service (platform agn.)  │  │ installation service   │
   └───────┬───────┘  └──────────────────────────┘  └────────────────────────┘
           │ events (token / message / tap)
   ┌───────▼───────────────────────────────┐
   │ Platform entry points                 │
   │ Android: TestAppFirebaseMessagingSvc  │
   │ iOS:     AppDelegate / UNUserNotif.   │
   └───────────────────────────────────────┘
```

Two rules keep the view model platform agnostic:

1. The view model never references `FirebaseMessaging`, `UIKit` or `Android.*`.
2. The view model never performs HTTP calls. It calls `INotificationRegistrationService`.

## 5. The view model (the key change)

`PushNotificationViewModel` exposes observable state (`DeviceToken`, `DeviceId`,
`StatusMessage`, `IsRegistered`, `IsBusy`, `LastMessageTitle`, `LastMessageBody`) and
three commands.

### 5.1 `InitializeAsync` (was `OnCreate`)

```csharp
public async Task InitializeAsync()
{
    if (IsBusy) return;

    try
    {
        IsBusy = true;
        Subscribe();                                  // token / message / tap events

        DeviceId = _deviceInstallationService.GetDeviceId();

        // Restore the last known registration so returning users see their state immediately.
        var cachedToken = await _secureStorageService.GetCacheItemAsync(Constants.PushToken);
        DeviceToken = cachedToken;
        IsRegistered = !string.IsNullOrWhiteSpace(cachedToken);

        if (!_deviceInstallationService.NotificationsSupported) { ... return; }

        if (!await _pushNotificationService.RequestPermissionAsync()) { ... return; }

        var token = await _pushNotificationService.GetPushChannelTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            // Keep the cached state; the last known token may still be valid server side.
            StatusMessage = IsRegistered
                ? "Using the last known push token; the live token could not be resolved."
                : "Unable to resolve a push channel token.";
            return;
        }

        await ApplyTokenAsync(token);                 // RefreshRegistrationAsync under the hood
    }
    finally { IsBusy = false; }
}
```

The cached token is not dead code: it seeds `DeviceToken` and `IsRegistered` before the live
token is resolved, and it survives as a fallback when the live token cannot be obtained. It is
only ever written on a successful registration (in `ApplyTokenAsync`) and removed on
deregistration, so its presence means the last attempt succeeded.

`ApplyTokenAsync` is the single place that writes the token and calls the backend:

```csharp
private async Task ApplyTokenAsync(string token)
{
    DeviceToken = token;
    _deviceInstallationService.Token = token;                 // same effect as the original line

    var result = await _notificationRegistrationService.RefreshRegistrationAsync(token);

    IsRegistered = result.IsSuccess;
    StatusMessage = result.IsSuccess ? "Device registered…" : $"Registration failed: {…}";

    if (result.IsSuccess)
    {
        await _secureStorageService.SaveItemToCacheAsync(Constants.PushToken, token);
    }
}
```

### 5.2 `DeregisterAsync`

Calls `INotificationRegistrationService.DeregisterDeviceAsync` and updates the UI.

### 5.3 Token refresh

`TokenRefreshed` is raised by the platform push service and routed through the same
`ApplyTokenAsync`, so a refreshed token always re-runs the registration.

### 5.4 Triggering from the page

```csharp
// Views/MainPage.xaml.cs
protected override async void OnAppearing()
{
    base.OnAppearing();
    await _viewModel.InitializeAsync();
}
```

This is the direct replacement for the block that used to sit in `MainActivity.OnCreate`.

## 6. Android

### 6.1 `AndroidDeviceInstallationService`

`IDeviceInstallationService` implementation. Unchanged in behaviour from the original:
`NotificationsSupported` uses `GoogleApiAvailability`, `GetDeviceId` reads
`Secure.AndroidId`, and `GetDeviceInstallation` builds the DTO with
`Platform = NotificationPlatform.FcmV1`.

### 6.2 `AndroidPushNotificationService` (new)

`IPushNotificationService` implementation. This is the class the view model talks to.

- `RequestPermissionAsync` requests `POST_NOTIFICATIONS` on Android 13+ and returns `true`
  on older versions.
- `GetPushChannelTokenAsync` wraps `FirebaseMessaging.Instance.GetToken()` and adapts the
  Google Play Services `Task` to a .NET `Task<string>` using the three listener classes.
- `RaiseTokenRefreshed` / `RaiseNotificationReceived` / `RaiseNotificationOpened` implement
  `IPushNotificationEventSink` so platform entry points can push events back to the view model.

```csharp
public async Task<string> GetPushChannelTokenAsync()
{
    var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

    FirebaseMessaging.Instance.GetToken()
        .AddOnSuccessListener(new PNSTokenSuccessListener(completion.TrySetResult))
        .AddOnFailureListener(new PNSTokenFailureListener(e =>
            completion.TrySetException(new InvalidOperationException($"FCM token request failed: {e?.Message}"))))
        .AddOnCompleteListener(new PNSTokenCompleteListener(t =>
        {
            if (t.IsComplete && !t.IsSuccessful)
                completion.TrySetException(new InvalidOperationException("FCM token request did not complete."));
        }));

    return await completion.Task;
}
```

### 6.3 The `PNSToken*Listener` classes

These are the same bridge classes as the original sample. Each is a `Java.Lang.Object`
that implements one Google Play Services listener interface and forwards to an `Action`.

| Class | Interface | Purpose |
| --- | --- | --- |
| `PNSTokenSuccessListener` | `IOnSuccessListener` | Unwraps the FCM token and invokes `Action<string>` |
| `PNSTokenFailureListener` | `IOnFailureListener` | Forwards the exception to `Action<Java.Lang.Exception>` |
| `PNSTokenCompleteListener` | `IOnCompleteListener` | Forwards the completed task to `Action<Android.Gms.Tasks.Task>` |

### 6.4 `TestAppFirebaseMessagingService`

Same attributes and manifest intent filter as the original
(`com.google.firebase.MESSAGING_EVENT`). Differences:

- `OnNewToken` no longer calls the registration service directly. It raises
  `IPushNotificationEventSink.RaiseTokenRefreshed`. The view model then re-registers.
- `OnMessageReceived` maps the FCM data payload to `PushMessageDto`, raises
  `RaiseNotificationReceived`, and builds the local notification.
- The `NotificationCompat` builder is unchanged (`maui_splash` icon, `PriorityMax`,
  `ChannelId`).

### 6.5 `MainActivity`

`OnCreate` now does only:

1. `CreateNotificationChannel()` (unchanged channel id/description from `Helpers.Constants`).
2. Subscribe to the `CloseApp` message.
3. `HandleIntent(Intent)` to forward a deep link.

`OnNewIntent` was added because the activity is `SingleTop`; a tapped notification reuses
the instance and would otherwise lose its extras.

> The registration block from the original `OnCreate` is gone. That is the whole point of
> the change.

### 6.6 Manifest and permissions

`Platforms/Android/AndroidManifest.xml` adds:

- `android.permission.POST_NOTIFICATIONS` (Android 13+)
- `com.google.android.c2dm.permission.RECEIVE`
- `android.permission.INTERNET`, `ACCESS_NETWORK_STATE`, `WAKE_LOCK`

## 7. iOS

iOS has no equivalent of an FCM SDK token call. APNs pushes the device token to the
app delegate, so the flow is event based.

### 7.1 `iOSDeviceInstallationService`

`IDeviceInstallationService` implementation. `GetDeviceId` uses
`UIDevice.CurrentDevice.IdentifierForVendor` and `GetDeviceInstallation` uses
`Platform = NotificationPlatform.ApnsV1`.

### 7.2 `iOSPushNotificationService` (new)

`IPushNotificationService` + `IPushNotificationEventSink` implementation.

- `RequestPermissionAsync` calls `UNUserNotificationCenter.RequestAuthorizationAsync`.
- `GetPushChannelTokenAsync` calls `UIApplication.RegisterForRemoteNotifications()` and
  awaits a `TaskCompletionSource<string>` that the app delegate completes.
- `RegisterDeviceToken(NSData)` formats the token bytes and completes the task.
- `RegisterFailed(NSError)` faults the task.

### 7.3 `AppDelegate`

- `FinishedLaunching` sets `UNUserNotificationCenter.Current.Delegate`.
- `RegisteredForRemoteNotifications` forwards the token to `iOSPushNotificationService`.
- `FailedToRegisterForRemoteNotifications` forwards the error to `iOSPushNotificationService`.
- `ReceivedRemoteNotification` shows the payload and raises `RaiseNotificationReceived`.

No registration work happens in the delegate, mirroring the Android change.

### 7.4 `PushNotificationDelegate`

`UNUserNotificationCenterDelegate` for the app:

- `WillPresentNotification` shows banner, list, badge and sound while the app is in the
  foreground and raises `RaiseNotificationReceived`.
- `DidReceiveNotificationResponse` handles a tap (including deep links) and raises
  `RaiseNotificationOpened` through `PushMessageFactory` / `NSDataExtensions`.

Both callbacks forward to `IPushNotificationEventSink`; the view model decides what to do.

### 7.5 Supporting iOS files

- `Program.cs` — entry point (`UIApplication.Main(args, null, typeof(AppDelegate))`).
- `PushMessageFactory.cs` — maps an APNs `NSDictionary` payload to `PushMessageDto`.
- `NSDataExtensions.cs` — `ToHexString` for the APNs device token and
  `ShowRemoteNotification` for the payload.
- `BadgeShellRenderer.cs` — `ShellRenderer` that shows a native tab bar badge driven by
  `UpdateBadgeMessage` (published by the view model when a push arrives).
- `PlainShellRenderer.cs` — renderer with a plain tab bar appearance.

### 7.6 `Info.plist` and `Entitlements.plist`

- `Info.plist` declares `UIBackgroundModes = remote-notification`.
- `Entitlements.plist` sets `aps-environment`. Use `production` for release builds.
- The sample ships per-configuration copies (`Info.Debug.plist`, `Entitlements.Debug.plist`)
  and the `.csproj` selects them explicitly:

```xml
<AppBundleManifest Condition="'$(Configuration)' == 'Debug'">Platforms/iOS/Info.Debug.plist</AppBundleManifest>
<AppBundleManifest Condition="'$(Configuration)' != 'Debug'">Platforms/iOS/Info.plist</AppBundleManifest>
<CodesignEntitlements Condition="'$(Configuration)' == 'Debug'">Platforms/iOS/Entitlements.Debug.plist</CodesignEntitlements>
<CodesignEntitlements Condition="'$(Configuration)' != 'Debug'">Platforms/iOS/Entitlements.plist</CodesignEntitlements>
```

Setting `AppBundleManifest` also disables the MAUI auto-detection of `Platforms/iOS/Info.plist`,
which is what allows the per-configuration copies to take effect.

### 7.7 Compiling the iOS files without macOS

The iOS target needs the `ios` workload and Xcode, so `dotnet build -f net10.0-ios` only runs on
macOS. The iOS files can still be type checked anywhere by compiling them against the
`Microsoft.iOS.Ref` reference assembly and the MAUI iOS reference assemblies:

```bash
# compile Platforms/iOS/*.cs + the small set of shared types it depends on
dotnet build check.csproj   # TargetFramework net10.0, Reference -> Microsoft.iOS.dll + MAUI libs
```

This catches missing `using` directives and API signature mismatches. It is how the
`LoggingService` import in `PushNotificationDelegate.cs` was found and fixed.

## 8. Shared services

### 8.1 `NotificationRegistrationService`

Same public surface as the original file, moved to the sample project:

- `RefreshRegistrationAsync(string)` — re-registers when the cached token differs.
- `RegisterDeviceAsync(string tags)` — posts to `{BasePushNotificationsUrl}/installations`.
- `DeregisterDeviceAsync()` — deletes `{BasePushNotificationsUrl}/installations/{deviceId}`.
- `SaveDeviceRegistrationDetailsAsync`, `DeleteDeviceRegistrationDetailsAsync`,
  `IsDeviceRegisteredAsync`.

Changes from the original:

- Implements `INotificationRegistrationService`.
- `RefreshRegistrationAsync` returns `NotificationRegistrationResult` instead of `Task`, so
  the view model can show a message without catching exceptions from `ContinueWith`.
- Failures are returned as results (and logged) instead of being rethrown, so a failed
  registration cannot crash the UI thread.
- Uses `System.Text.Json` through `IHttpCommunication` instead of `Newtonsoft.Json`.

### 8.2 Supporting services

| Service | Responsibility |
| --- | --- |
| `SecureStorageService` | `ISecureStorageService` over `SecureStorage.Default` (token, tags, device id) |
| `ConfigurationService` | `IConfigurationService.BasePushNotificationsUrl` |
| `HttpCommunication` | `IHttpCommunication` over a single `HttpClient`, bearer token support |
| `LoggingService<T>` | `LoggingService<NotificationRegistrationService>.LogError(ex)` facade |
| `MessageBus` | Channel based wrapper over `WeakReferenceMessenger` |

## 9. Dependency injection

`MauiProgram.CreateMauiApp` registers shared services, then platform services under
`#if ANDROID` / `#elif IOS`.

```csharp
services.AddSingleton<PushNotificationViewModel>();
services.AddSingleton<IDeviceInstallationService, AndroidDeviceInstallationService>();
services.AddSingleton<AndroidPushNotificationService>();
services.AddSingleton<IPushNotificationService>(sp => sp.GetRequiredService<AndroidPushNotificationService>());
services.AddSingleton<IPushNotificationEventSink>(sp => sp.GetRequiredService<AndroidPushNotificationService>());
```

The platform service is registered twice through **the same instance** because the view
model subscribes to `IPushNotificationService` events while the FCM service / app delegate
raises them through `IPushNotificationEventSink`.

`ServiceHelper.GetService<T>()` resolves from `IPlatformApplication.Current.Services`, which
is how `MainActivity`, `TestAppFirebaseMessagingService` and the iOS `AppDelegate` obtain
their dependencies without constructor injection.

## 10. Registration sequence

### Android (first launch)

```
MainPage.OnAppearing
  → PushNotificationViewModel.InitializeAsync
      → Subscribe()  (TokenRefreshed / NotificationReceived / NotificationOpened)
      → AndroidDeviceInstallationService.GetDeviceId()
      → AndroidPushNotificationService.RequestPermissionAsync()   (POST_NOTIFICATIONS, 13+)
      → AndroidPushNotificationService.GetPushChannelTokenAsync()
            → FirebaseMessaging.Instance.GetToken()
            → PNSTokenSuccessListener → TaskCompletionSource.SetResult(token)
      → ApplyTokenAsync(token)
            → DeviceInstallationService.Token = token
            → NotificationRegistrationService.RefreshRegistrationAsync(token)
                  → POST {BasePushNotificationsUrl}/installations
```

### Android (token rotated)

```
FCM → TestAppFirebaseMessagingService.OnNewToken(token)
    → IPushNotificationEventSink.RaiseTokenRefreshed(token)
    → PushNotificationViewModel.OnTokenRefreshed
    → ApplyTokenAsync(token)  (same path as above)
```

### iOS (first launch)

```
MainPage.OnAppearing
  → PushNotificationViewModel.InitializeAsync
      → iOSPushNotificationService.RequestPermissionAsync()
      → iOSPushNotificationService.GetPushChannelTokenAsync()
            → UIApplication.RegisterForRemoteNotifications()
            → AppDelegate.RegisteredForRemoteNotifications(NSData)
                  → iOSPushNotificationService.RegisterDeviceToken  → TaskCompletionSource
      → ApplyTokenAsync(token) → RefreshRegistrationAsync(token)
```

## 11. Setup

### 11.1 Prerequisites

- .NET 10 SDK
- MAUI workload: `dotnet workload install maui-android maui-ios`
- JDK 17 and the Android SDK (for Android), Xcode 16+ on macOS (for iOS)

### 11.2 Android

1. Create a Firebase project and add an Android app with package name
   `com.testapp.my.mobile`.
2. Download `google-services.json` and place it at
   `TestApp.My.Mobile/Platforms/Android/google-services.json`
   (`google-services.json.template` shows the expected shape).
3. Build and deploy:

```bash
dotnet build TestApp.My.Mobile/TestApp.My.Mobile.csproj -f net10.0-android
dotnet build TestApp.My.Mobile/TestApp.My.Mobile.csproj -t:Run -f net10.0-android
```

#### Verified build environment

The Android target was compiled and verified on Linux with:

| Tool | Version |
| --- | --- |
| .NET SDK | 10.0.403 |
| MAUI workload | `maui-android` |
| Android SDK platform | API 36 (`Microsoft.Android.Sdk.Linux 36.1.2`) |
| Build tools | 36.0.0 |
| JDK | OpenJDK 21 |

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
export JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64
export ANDROID_HOME="$HOME/.android-sdk"
dotnet build TestApp.My.Mobile/TestApp.My.Mobile.csproj \
    -f net10.0-android -p:TargetFrameworks=net10.0-android \
    -p:AndroidSdkDirectory="$ANDROID_HOME"
```

Result: `Build succeeded` with 0 errors.

Two things are worth calling out because they are easy to hit on a fresh machine:

- **Android SDK components.** `dotnet build -t:InstallAndroidDependencies` fails in a container
  where the `USER` environment variable is empty (`CommonUtilities.Helpers.UserName must have a
  valid value`). Export `USER=...` before running it, or install the SDK with
  `sdkmanager` directly.
- **AndroidX duplicate classes.** MAUI 10 resolves `Xamarin.AndroidX.Fragment` 1.9.x (which
  merged the KTX extension classes into the main artifact) while some transitive dependencies
  still ask for `Xamarin.AndroidX.Fragment.Ktx` 1.8.x. Both then contain
  `androidx/fragment/app/FragmentKt.class` and R8 fails with
  `Type ... FragmentKt is defined multiple times`. The `.csproj` pins
  `Xamarin.AndroidX.Fragment.Ktx` to `1.9.0` and `Xamarin.AndroidX.Collection.Ktx` to `1.6.0.1`
  to keep the graph consistent. The same reasoning applies to
  `Xamarin.AndroidX.Collection.Ktx` / `Collection.Jvm`.

The remaining build warnings are `NU1608` entries about transitive AndroidX version ranges
(e.g. `Lifecycle.LiveData` 2.9.2.1 vs `Lifecycle.LiveData.Core` 2.11.0.1). They come from the
MAUI 10 dependency graph and do not affect the build.

### 11.3 iOS

1. In the Apple Developer portal create an App ID with the Push Notifications capability
   and an APNs key or certificate.
2. Set the bundle identifier to match `ApplicationId` (`com.testapp.my.mobile`).
3. Build (macOS only):

```bash
dotnet build TestApp.My.Mobile/TestApp.My.Mobile.csproj -f net10.0-ios
```

### 11.4 Backend

Set `ConfigurationService.BasePushNotificationsUrl` to your registration API. The API is
expected to expose:

| Method | Route | Body |
| --- | --- | --- |
| `POST` | `/installations` | `UserDeviceRegistrationRequestDto` |
| `DELETE` | `/installations/{installationId}` | — |
| `POST` | `/DeviceRegistration/register` | `DeviceRegistrationDto` |
| `POST` | `/DeviceRegistration/unregister` | device token |
| `GET` | `/DeviceRegistration/isdeviceregistered/{token}` | — |

Send notifications with `Platform = FcmV1` for Android and `ApnsV1` for iOS.

## 12. Manual test matrix

| # | Scenario | Expected result |
| --- | --- | --- |
| 1 | Android 13+ first launch | Permission dialog appears, token shown, device registered |
| 2 | Android 12 first launch | No dialog, token shown, device registered |
| 3 | Permission denied | `StatusMessage` = permission denied, no HTTP call |
| 4 | Token rotated (clear app data) | `OnNewToken` fires, device re-registered |
| 5 | Foreground message | `LastMessageTitle` / `LastMessageBody` update, local notification shown |
| 6 | Tap notification with deep link | `NotificationOpened` → `SendOnAppLinkRequestReceived` |
| 7 | Deregister | `DELETE /installations/{id}`, cached token removed |
| 8 | iOS first launch | Permission dialog, APNs token shown, device registered as `ApnsV1` |
| 9 | Airplane mode | Registration fails, message shown, app does not crash |
| 10 | Reopen app with unchanged token | No duplicate registration (`RefreshRegistrationAsync` short-circuits) |

## 13. Original vs new

| Concern | Original | New |
| --- | --- | --- |
| Registration trigger | `MainActivity.OnCreate` | `MainPage.OnAppearing` → view model |
| Token acquisition | Inline in `OnCreate` | `IPushNotificationService.GetPushChannelTokenAsync` |
| Token refresh | `TestAppFirebaseMessagingService.OnNewToken` + `ContinueWith` | `TokenRefreshed` event → `ApplyTokenAsync` |
| Registration call | `ContinueWith` + `throw task.Exception` | `await RefreshRegistrationAsync`, returns a result |
| UI feedback | none | `StatusMessage`, `IsBusy`, `IsRegistered` |
| Platform code in view model | n/a (no view model) | none (abstractions only) |
| iOS | not covered | full APNs path |

## 14. Extending

- **Tags**: pass a comma separated tag string to `RegisterDeviceAsync`; it is split into a
  list and sent in the registration request.
- **Deep links**: add URL handling in `MainPage` (or a shell route) and subscribe to
  `IPushNotificationService.NotificationOpened`.
- **Unit tests**: `PushNotificationViewModel` can be tested with in-memory implementations of
  `IPushNotificationService`, `IDeviceInstallationService`, `INotificationRegistrationService`
  and `ISecureStorageService`. No MAUI platform code is needed.
- **Localization**: move the strings in `StatusMessage` into resource files.
