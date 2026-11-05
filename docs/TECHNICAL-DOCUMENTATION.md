# PushNotification Sample App — Technical Documentation

Complete reference for the .NET 10 MAUI push notification sample: architecture, every
public method, the platform integration points, and the end-to-end processes.

## Table of contents

1. [Overview](#1-overview)
2. [Repository layout](#2-repository-layout)
3. [Project and build configuration](#3-project-and-build-configuration)
4. [Application startup and dependency injection](#4-application-startup-and-dependency-injection)
5. [Data models](#5-data-models)
6. [Service reference](#6-service-reference)
7. [Platform integration](#7-platform-integration)
8. [View models](#8-view-models)
9. [Views](#9-views)
10. [End-to-end processes](#10-end-to-end-processes)
11. [Configuration reference](#11-configuration-reference)
12. [Backend contract](#12-backend-contract)
13. [Replacing the dummy services](#13-replacing-the-dummy-services)
14. [Build, run and troubleshooting](#14-build-run-and-troubleshooting)
15. [Known limitations](#15-known-limitations)

---

## 1. Overview

This sample demonstrates a complete push notification implementation for a .NET 10 MAUI
mobile app on iOS and Android:

| Concern | Android | iOS |
| --- | --- | --- |
| Push transport | Firebase Cloud Messaging (FCM) | Apple Push Notification service (APNs) |
| Token source | `FirebaseMessaging.Instance.GetToken()` | `UIApplication.RegisterForRemoteNotifications` callback |
| Message receipt | `FirebaseMessagingService.OnMessageReceived` | `UNUserNotificationCenterDelegate.WillPresentNotification` |
| Notification tap | `MainActivity` launch intent extras | `UNUserNotificationCenterDelegate.DidReceiveNotificationResponse` |
| Runtime permission | `POST_NOTIFICATIONS` (API 33+) | `UNUserNotificationCenter.RequestAuthorization` |
| Topic model | FCM topics (server-side subscription) | Backend-enforced custom key |

The app also provides:

- A **guided permission flow** started from a button on the Home screen. The app first
  shows an in-app soft prompt explaining why notifications are useful; only when the user
  agrees does it show the operating system permission dialog. When the user allows the OS
  permission the notification preference view opens as a popup.
- A **notification preference screen** where the user opts in or out of six notification
  topics, with a master switch.
- A **device information screen** that displays the installation record and exposes
  register / save / unregister actions.
- **Dummy backend services** that log the exact payloads a real API would receive, so the
  whole flow is exercisable without a server.

### Design principles

1. **Platform abstraction.** All shared code depends on `IPushNotificationService` and
   `INotificationPermissionService`. Only the registrations in `MauiProgram` differ per
   target, so no `#if` directives leak into view models or services.
2. **OS callbacks are bridged.** FCM services and the iOS `AppDelegate` are instantiated by
   the operating system and cannot use constructor injection. Static bridge classes
   (`FirebasePushBridge`, `ApplePushBridge`) fan events out to the singleton push service
   that lives in the DI container.
3. **Failures are non-fatal.** A failed topic subscription or backend sync is logged and
   does not block saving the user's local choice.
4. **Desktop targets stay runnable.** Windows and Mac Catalyst register fallback
   implementations so the shared UI compiles and runs everywhere.
5. **Permission is asked in context.** The soft prompt is shown before the OS dialog so the
   user understands the value first, which is the pattern Apple and Google recommend. The OS
   dialog is only shown after the user agrees, and the preference popup follows a grant.

---

## 2. Repository layout

```
PushNotification.slnx                        Solution file (XML solution format)
README.md                                    Getting started guide
docs/TECHNICAL-DOCUMENTATION.md              This document
src/PushNotification.App/
  PushNotification.App.csproj                Project + per-platform package references
  appsettings.json                           Embedded push configuration
  MauiProgram.cs                             DI composition root
  App.xaml / App.xaml.cs                     Application shell host
  AppShell.xaml / AppShell.xaml.cs           Tab bar navigation
  .gitignore                                 Build outputs + google-services.json
  Configuration/
    PushOptions.cs                           Strongly typed configuration
  Models/
    DeviceInstallation.cs                    Installation snapshot
    DeviceRegistrationRequest.cs             Registration payload
    DeviceRegistrationResult.cs              Backend response + factories
    NotificationPermissionStatus.cs          Permission enum
    NotificationPreference.cs                Topic preference value object
    NotificationTopic.cs                     Topic enum
    PlatformType.cs                          Platform enum
    PushConfiguration.cs                     Runtime provider configuration
    PushMessage.cs                           Normalized received message
    PushPermissionOutcome.cs                 Result of the guided permission flow
  Services/
    IDeviceInfoProvider.cs                   Device metadata contract
    DeviceInfoProvider.cs                    MAUI Essentials implementation
    PushRegistrationCoordinator.cs           Lifecycle orchestration
    PushPermissionFlowService.cs             Soft prompt -> OS prompt -> preference popup
    Api/
      IDeviceRegistrationService.cs          Backend contract
      DummyDeviceRegistrationService.cs      Logging implementation
    Notifications/
      IPushNotificationService.cs            Push contract
      INotificationPermissionService.cs      Permission contract
      UnsupportedPushNotificationService.cs  Desktop fallback (push)
      AlwaysGrantedNotificationPermissionService.cs  Desktop fallback (permission)
    Preferences/
      INotificationPreferencesService.cs     Preferences contract
      NotificationPreferencesService.cs      Persistence + subscription sync
  ViewModels/
    BaseViewModel.cs                         INotifyPropertyChanged base
    HomeViewModel.cs                         Home screen logic
    NotificationPreferencesViewModel.cs      Preferences screen logic
    NotificationPreferenceItem.cs            One toggle row
    DeviceInfoViewModel.cs                   Device screen logic + DeviceInfoRow
  Views/
    HomePage.xaml / .xaml.cs
    NotificationPreferencesView.xaml / .xaml.cs       Shared preference view
    NotificationPreferencesPage.xaml / .xaml.cs       Preferences tab host
    NotificationPreferencesPopupPage.xaml / .xaml.cs  Preference popup
    NotificationPreferencesDialogService.cs           Popup presenter
    DeviceInfoPage.xaml / .xaml.cs
  Platforms/
    Android/
      MainActivity.cs                        Permission callback + tap routing
      MainApplication.cs                     MAUI application entry
      AndroidManifest.xml                    Permissions
      AndroidPushNotificationService.cs      FCM implementation
      AndroidNotificationPermissionService.cs
      AppFirebaseMessagingService.cs         FCM receive callbacks
      FirebasePushBridge.cs                  OS-to-DI event fan-out
      FirebaseMessageMapper.cs               Payload to PushMessage mapping
      DeviceInfoProvider.Platform.cs         Model/manufacturer/emulator
      google-services.json.template          Reference Firebase config
    iOS/
      AppDelegate.cs                         APNs registration + UNUserNotificationCenter delegate
      ApplePushNotificationService.cs        APNs implementation
      AppleNotificationPermissionService.cs
      ApplePushBridge.cs                     OS-to-DI event fan-out
      DeviceInfoProvider.Platform.cs         Model via sysctl hw.machine
      Info.plist                             UIBackgroundModes: remote-notification
    MacCatalyst/                             Fallback + platform device details
    Windows/                                 Fallback + platform device details
```

---

## 3. Project and build configuration

### 3.1 Target frameworks

`PushNotification.App.csproj` declares target frameworks conditionally on the build host:

```xml
<TargetFrameworks>net10.0-android</TargetFrameworks>
<TargetFrameworks Condition="!$([MSBuild]::IsOSPlatform('linux'))">$(TargetFrameworks);net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
```

On Linux only Android builds. iOS and Mac Catalyst are added on macOS; Windows is added on
Windows. This keeps CI on any host green without excluding code paths.

### 3.2 Key properties

| Property | Value | Reason |
| --- | --- | --- |
| `RootNamespace` | `PushNotification.App` | Namespace root for all shared code |
| `UseMaui` / `SingleProject` | `true` | Single-project MAUI layout |
| `Nullable` | `enable` | Nullable reference types enforced |
| `MauiXamlInflator` | `SourceGen` | XAML compiled at build time |
| `ApplicationId` | `com.companyname.pushnotification.app` | Must match the Firebase Android app id |
| `SupportedOSPlatformVersion` (Android) | `24.0` | Required by `androidx.tracing` pulled in by FCM |
| `SupportedOSPlatformVersion` (iOS) | `15.0` | Minimum for the notification APIs used |
| `WindowsPackageType` | `None` | Unpackaged Windows runs |

### 3.3 Package references

Shared:

- `Microsoft.Maui.Controls`
- `Microsoft.Extensions.Logging.Debug`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Configuration.Binder`
- `Microsoft.Extensions.Configuration.EnvironmentVariables`
- `Microsoft.Extensions.Options.ConfigurationExtensions`

Android only (`Xamarin.Firebase.Messaging` 125.1.3 plus AndroidX pins):

```xml
<PackageReference Include="Xamarin.Firebase.Messaging" Version="125.1.3" />
<PackageReference Include="Xamarin.AndroidX.Fragment.Ktx" Version="1.9.0" />
<PackageReference Include="Xamarin.AndroidX.Collection.Ktx" Version="1.6.0.1" />
<PackageReference Include="Xamarin.AndroidX.Lifecycle.LiveData" Version="2.11.0.1" />
<PackageReference Include="Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx" Version="2.11.0.1" />
<PackageReference Include="Xamarin.AndroidX.Lifecycle.Process" Version="2.11.0.1" />
```

**Why the AndroidX pins exist.** `Xamarin.Firebase.Messaging` declares dependencies on
older AndroidX versions (for example `Xamarin.AndroidX.Fragment` 1.8.8.1 and
`Xamarin.AndroidX.Collection.Jvm` 1.5.0.3) while MAUI 10 resolves newer ones (1.9.0 and
1.6.0.1). NuGet picks the higher version, but the older companion artifacts still bring in
their own copies of the same Java classes, so `d8` fails during dexing with
`Type androidx.fragment.app.FragmentKt is defined multiple times`. Pinning the companion
packages to the versions that match the resolved AndroidX libraries makes the graph
consistent. If you upgrade MAUI or Firebase, re-check these pins.

### 3.4 Firebase config injection

```xml
<GoogleServicesJson Include="Platforms/Android/google-services.json"
                    Condition="Exists('Platforms/Android/google-services.json')" />
```

The real `google-services.json` is optional at build time and gitignored. When present, the
`Xamarin.GooglePlayServices.Basement` build target processes it and injects the Firebase
values into the APK. Without it the app still builds and runs, but FCM cannot issue a token.

### 3.5 iOS runtime identifier

```xml
<PropertyGroup Condition="... == 'ios' and '$(RuntimeIdentifier)' == '' and '$(Configuration)' == 'Debug'">
  <RuntimeIdentifier>iossimulator-x64</RuntimeIdentifier>
</PropertyGroup>
```

Debug iOS builds default to the simulator runtime so `dotnet build` works without device
provisioning. Note that push notifications do not function on the simulator.

---

## 4. Application startup and dependency injection

### 4.1 `MauiProgram.CreateMauiApp()`

The composition root. Sequence:

1. `MauiApp.CreateBuilder()`.
2. `UseMauiApp<App>()` and font registration.
3. **Configuration loading.**
   - Locate the embedded `appsettings.json` by scanning
     `Assembly.GetManifestResourceNames()` for a name ending in `appsettings.json`
     (case insensitive). This is deliberately name-agnostic because the MSBuild logical
     name is prefixed with the root namespace, producing `PushNotification.App.appsettings.json`.
   - `builder.Configuration.AddJsonStream(stream)`.
   - `builder.Configuration.AddEnvironmentVariables("PUSH_")` so environment variables
     override file values.
   - `builder.Services.Configure<PushOptions>(...)` binding the `Push` section.
4. `RegisterPlatformServices(services)` — see below.
5. `RegisterSharedServices(services)` — see below.
6. `builder.Logging.AddDebug()` in `DEBUG`.
7. `builder.Build()`.

### 4.2 `RegisterPlatformServices(IServiceCollection)`

Registers the push and permission implementations for the current target using
compile-time symbols:

```csharp
#if ANDROID
    services.AddSingleton<IPushNotificationService, AndroidPushNotificationService>();
    services.AddSingleton<INotificationPermissionService, AndroidNotificationPermissionService>();
#elif IOS
    services.AddSingleton<IPushNotificationService, ApplePushNotificationService>();
    services.AddSingleton<INotificationPermissionService, AppleNotificationPermissionService>();
#else
    services.AddSingleton<IPushNotificationService, UnsupportedPushNotificationService>();
    services.AddSingleton<INotificationPermissionService, AlwaysGrantedNotificationPermissionService>();
#endif
```

### 4.3 `RegisterSharedServices(IServiceCollection)`

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `IPreferences` | `Preferences.Default` (instance registration) | Singleton |
| `IDeviceInfoProvider` | `DeviceInfoProvider` | Singleton |
| `IDeviceRegistrationService` | `DummyDeviceRegistrationService` | Singleton |
| `INotificationPreferencesService` | `NotificationPreferencesService` | Singleton |
| `IPushRegistrationCoordinator` | `PushRegistrationCoordinator` | Singleton |
| `IPushPermissionFlowService` | `PushPermissionFlowService` | Singleton |
| `INotificationPreferencesDialogService` | `NotificationPreferencesDialogService` | Singleton |
| `AppShell` | `AppShell` | Singleton |
| `HomeViewModel` / `HomePage` | — | Singleton |
| `NotificationPreferencesViewModel` / `NotificationPreferencesPage` | — | Singleton |
| `DeviceInfoViewModel` / `DeviceInfoPage` | — | Singleton |

### 4.4 `App` and `AppShell`

`App` receives `IServiceProvider`, and `CreateWindow` resolves `AppShell` from the container
and wraps it in a `Window`. `AppShell` defines a `TabBar` with three routes:

| Route | Page | Title |
| --- | --- | --- |
| `home` | `HomePage` | Home |
| `preferences` | `NotificationPreferencesPage` | Preferences |
| `device` | `DeviceInfoPage` | Device |

Each `ShellContent` uses `ContentTemplate="{DataTemplate views:...}"`, so pages are created
on first navigation and resolved through DI.

---

## 5. Data models

### 5.1 `DeviceInstallation` (`Models/DeviceInstallation.cs`)

The point-in-time installation snapshot sent to the backend.

| Member | Type | Description |
| --- | --- | --- |
| `InstallationId` | `string` | Stable per-installation GUID, persisted in `IPreferences` |
| `DeviceToken` | `string` | FCM registration token or APNs device token |
| `Platform` | `PlatformType` | Enum platform |
| `PlatformName` | `string` | Raw platform string, e.g. `Android` |
| `Model` | `string` | Hardware model, e.g. `Pixel 8` or `iPhone15,2` |
| `Manufacturer` | `string` | e.g. `Google`, `Apple` |
| `DeviceName` | `string` | User-facing device name |
| `OsVersion` | `string` | e.g. `Android 15`, `iOS 18.1` |
| `AppVersion` | `string` | Display version |
| `AppBuild` | `string` | Build number |
| `BundleId` | `string` | Package/bundle identifier |
| `Language` | `string` | Two-letter ISO language code |
| `TimeZone` | `string` | IANA time zone id |
| `Locale` | `string` | Locale identifier, e.g. `en-US` |
| `ScreenResolution` | `string` | `width x height` in device-independent pixels |
| `ScreenDensity` | `double` | Pixel density |
| `DeviceType` | `string` | Idiom: `Phone`, `Tablet`, `Desktop` |
| `IsEmulator` | `bool` | Platform-reported simulator/emulator flag |
| `CapturedAtUtc` | `DateTimeOffset` | Snapshot timestamp, defaults to now |

### 5.2 `DeviceRegistrationRequest` (`Models/DeviceRegistrationRequest.cs`)

| Member | Type | Description |
| --- | --- | --- |
| `InstallationId` | `string` | Installation identifier |
| `DeviceToken` | `string` | Push token |
| `Platform` | `PlatformType` | Platform |
| `UserId` | `string?` | Signed-in user; `null` for anonymous installs |
| `Topics` | `List<string>` | Topic names enabled at registration time |
| `AppVersion` | `string` | Version that produced the token |

### 5.3 `DeviceRegistrationResult` (`Models/DeviceRegistrationResult.cs`)

| Member | Type | Description |
| --- | --- | --- |
| `Success` | `bool` | Whether the operation succeeded |
| `InstallationId` | `string?` | Server-side identifier when known |
| `Message` | `string?` | Human readable status or error |
| `ProcessedAtUtc` | `DateTimeOffset` | Response timestamp |

Factories:

- `static DeviceRegistrationResult Ok(string installationId, string message = "Registered")`
- `static DeviceRegistrationResult Fail(string message)`

### 5.4 `NotificationTopic` (`Models/NotificationTopic.cs`)

Enum with six members. `ToString()` is the canonical topic name used for FCM topics and in
backend payloads: `Announcements`, `AccountAlerts`, `OrderUpdates`, `Promotions`, `Tips`,
`WeeklyDigest`.

### 5.5 `NotificationPreference` (`Models/NotificationPreference.cs`)

Immutable value object constructed as
`new NotificationPreference(NotificationTopic topic, string displayName, string description)`
with a mutable `IsEnabled` property. `Topic`, `DisplayName` and `Description` are
get-only.

### 5.6 `PlatformType` (`Models/PlatformType.cs`)

`Unknown = 0`, `Android = 1`, `iOS = 2`, `Windows = 3`, `MacCatalyst = 4`.

### 5.7 `NotificationPermissionStatus` (`Models/NotificationPermissionStatus.cs`)

`NotRequested = 0`, `Granted = 1`, `Denied = 2`, `Suppressed = 3`, `Unknown = 4`.

`Suppressed` exists for platforms that report a granted permission with notifications
muted at the OS level; the sample's implementations do not currently produce it.

### 5.8 `PushConfiguration` (`Models/PushConfiguration.cs`)

Runtime provider configuration assembled by the coordinator from `PushOptions`:

| Member | Default | Purpose |
| --- | --- | --- |
| `FcmSenderId` | `null` | FCM sender id (project number) |
| `FcmNotificationChannelId` | `general` | Android notification channel id |
| `FcmNotificationChannelName` | `General` | Android channel display name |
| `ApnsEnvironment` | `development` | `development` or `production` |
| `ApnsTopic` | `string.Empty` | Bundle identifier for the APNs topic |
| `ApiBaseUrl` | `https://api.example.com` | Backend base URL |

### 5.9 `PushMessage` (`Models/PushMessage.cs`)

Normalized received message shared by both platforms:

| Member | Type | Description |
| --- | --- | --- |
| `MessageId` | `string?` | Provider message id |
| `Title` | `string?` | Notification title |
| `Body` | `string?` | Notification body |
| `Topic` | `string?` | Topic or sender, when reported |
| `Data` | `IDictionary<string, string>` | Custom key/value payload |
| `WasForeground` | `bool` | Whether the app was in the foreground |
| `ReceivedAtUtc` | `DateTimeOffset` | Receive timestamp |

### 5.10 `PushPermissionOutcome` (`Models/PushPermissionOutcome.cs`)

Enum describing how the guided permission flow finished:

| Value | Meaning |
| --- | --- |
| `Unavailable` | No visible page was available to host the soft prompt |
| `SoftPromptDeclined` | The user declined the soft prompt or dismissed it; the OS was never prompted |
| `OsPermissionDenied` | The operating system dialog reported a denial |
| `OsPermissionGranted` | The user allowed the OS permission; preferences may be shown |

The distinction between a soft prompt decline and an OS denial matters: a decline does not
count as an OS-level denial, so the app can still show the OS dialog later.

---

## 6. Service reference

### 6.1 `IDeviceInfoProvider` / `DeviceInfoProvider`

`Services/IDeviceInfoProvider.cs`, `Services/DeviceInfoProvider.cs`,
`Platforms/*/DeviceInfoProvider.Platform.cs`

`DeviceInfoProvider` is a `sealed partial class` built on MAUI Essentials. The platform
partial supplies hardware details; the shared part supplies everything else.

#### `string GetOrCreateInstallationId()`

Reads `pn.installation_id` from `IPreferences`. If absent or blank, generates
`Guid.NewGuid().ToString("D")`, persists it, and returns it. The value survives app
restarts and token refreshes but changes on uninstall/reinstall, which is the intended
semantics for an installation identifier.

#### `DeviceInstallation GetDeviceInfo()`

Builds the full snapshot:

1. `GetPlatform()` — maps `Microsoft.Maui.Devices.DeviceInfo.Current.Platform` to
   `PlatformType` with an explicit comparison chain (a `switch` on `DevicePlatform` is not
   legal because those are not compile-time constants).
2. `GetPlatformDetails()` — the partial method implemented per platform.
3. Reads `DeviceInfo.Current` for name, version and idiom.
4. Reads `AppInfo.Current` for version, build and package name.
5. Reads `CultureInfo` and `TimeZoneInfo` for locale data.
6. Reads `DeviceDisplay.MainDisplayInfo` for resolution and density.
7. Returns a populated `DeviceInstallation`.

#### `private static string GetScreenResolution()`

Formats `DeviceDisplay.MainDisplayInfo` as `"{Width}x{Height}"`.

#### `private partial PlatformDeviceDetails GetPlatformDetails()`

Declared in the shared file, implemented once per platform. `PlatformDeviceDetails` is a
`readonly record struct (string Model, string Manufacturer, bool IsEmulator)`.

- **Android** (`Platforms/Android/DeviceInfoProvider.Platform.cs`): uses `Android.OS.Build`.
  `Model` and `Manufacturer` come from `Build`. `IsEmulator` is true when
  `Build.Fingerprint` contains `generic`, `emulator` or `sdk` (case insensitive).
- **iOS** (`Platforms/iOS/DeviceInfoProvider.Platform.cs`): calls the native
  `sysctlbyname("hw.machine", ...)` via `[DllImport("libc")]` to get the hardware
  identifier such as `iPhone15,2`, falling back to `UIDevice.CurrentDevice.Model`.
  `Manufacturer` is `Apple`. `IsEmulator` is true when the machine id starts with
  `x86_64`, `i386` or `arm64`.
- **Windows** and **Mac Catalyst**: read `DeviceInfo.Current.Model` and `Manufacturer`
  (Mac Catalyst forces `Apple`); `IsEmulator` is `false`.

The native call is wrapped in `try`/`finally` with `Marshal.AllocHGlobal(128)` and
`Marshal.FreeHGlobal` so the unmanaged buffer is always released.

### 6.2 `IDeviceRegistrationService` / `DummyDeviceRegistrationService`

`Services/Api/IDeviceRegistrationService.cs`, `Services/Api/DummyDeviceRegistrationService.cs`

The backend contract. The dummy implementation performs no network I/O; it awaits a
150 ms delay to simulate latency and logs the JSON payload it would have sent, using
`JsonSerializerOptions` with `WriteIndented = true` and a `JsonStringEnumConverter`.

#### `Task<DeviceRegistrationResult> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default)`

- Throws `ArgumentNullException` when `request` is null.
- Returns `Fail("A device token is required to register.")` and logs a warning when
  `request.DeviceToken` is blank.
- Otherwise simulates the network delay, logs
  `[DUMMY] POST /api/devices/register` with the serialized payload, and returns
  `Ok(request.InstallationId, "Device token registered.")`.

#### `Task<DeviceRegistrationResult> SaveInstallationAsync(DeviceInstallation installation, CancellationToken cancellationToken = default)`

- Throws `ArgumentNullException` when `installation` is null.
- Logs `[DUMMY] PUT /api/installations/{InstallationId}` with the serialized record.
- Returns `Ok(installation.InstallationId, "Installation saved.")`.

#### `Task<DeviceRegistrationResult> UnregisterDeviceAsync(string installationId, CancellationToken cancellationToken = default)`

- Throws `ArgumentException` when `installationId` is null, empty or whitespace.
- Logs `[DUMMY] DELETE /api/installations/{InstallationId}`.
- Returns `Ok(installationId, "Installation removed.")`.

#### `Task<DeviceRegistrationResult> SaveNotificationPreferencesAsync(string installationId, IReadOnlyCollection<NotificationTopic> enabledTopics, CancellationToken cancellationToken = default)`

- Validates both arguments.
- Projects the topics to their string names.
- Logs `[DUMMY] PUT /api/installations/{InstallationId}/preferences` with
  `{ "topics": [...] }`.
- Returns `Ok(installationId, "Preferences saved.")`.

### 6.3 `IPushNotificationService`

`Services/Notifications/IPushNotificationService.cs`

The platform-independent push contract.

| Member | Signature | Purpose |
| --- | --- | --- |
| `TokenRefreshed` | `event EventHandler<string>` | New or rotated device token |
| `MessageReceived` | `event EventHandler<PushMessage>` | Message arrived while running |
| `NotificationTapped` | `event EventHandler<PushMessage>` | User opened the app from a notification |
| `InitializeAsync` | `Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken cancellationToken = default)` | Initialize the provider; idempotent |
| `GetDeviceTokenAsync` | `Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default)` | Current token or `null` |
| `SubscribeToTopicAsync` | `Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)` | Enable a topic |
| `UnsubscribeFromTopicAsync` | `Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)` | Disable a topic |
| `DeleteTokenAsync` | `Task DeleteTokenAsync(CancellationToken cancellationToken = default)` | Delete the token, e.g. at sign-out |

### 6.4 `AndroidPushNotificationService`

`Platforms/Android/AndroidPushNotificationService.cs`

FCM implementation. Holds a single `_initialized` flag so repeated `InitializeAsync` calls
do not attach duplicate bridge handlers.

#### `Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken cancellationToken = default)`

On first call, subscribes to `FirebasePushBridge.TokenRefreshed`, `.MessageReceived` and
`.NotificationTapped` and sets `_initialized`. Always calls
`CreateNotificationChannel(configuration)` and logs the sender id and channel. Returns
`Task.FromResult(true)`.

#### `Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default)`

Awaits `FirebaseMessaging.Instance.GetToken()` and converts the result with
`.AsAsync<Java.Lang.String>()`. Returns the token string, or `null` and a logged warning
on exception. The call is wrapped in `#pragma warning disable CS0618` because the binding
marks it obsolete although it remains the supported API.

#### `Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)`

Resolves the topic name via `TopicName(topic)` and awaits
`FirebaseMessaging.Instance.SubscribeToTopic(name)`. Exceptions are logged as warnings so a
provider failure never breaks the preference save.

#### `Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)`

Mirror of subscribe using `UnsubscribeFromTopic(name)`.

#### `Task DeleteTokenAsync(CancellationToken cancellationToken = default)`

Awaits `FirebaseMessaging.Instance.DeleteToken()`; logs and swallows failures.

#### `static string TopicName(NotificationTopic topic)`

Returns `topic.ToString().ToLowerInvariant()`, e.g. `accountalerts`. This is the canonical
FCM topic name; the backend must use the same value when publishing.

#### `private void CreateNotificationChannel(PushConfiguration configuration)`

No-ops below API 26. Otherwise resolves `NotificationManager` from the application context
and creates a channel with `NotificationImportance.Default`, description
"Push notifications from the app.", using the configured id and name. Skips creation when
the channel already exists.

#### Private handlers

`OnTokenRefreshed`, `OnMessageReceived` and `OnNotificationTapped` re-raise the bridge
events on the service's own public events.

### 6.5 `ApplePushNotificationService`

`Platforms/iOS/ApplePushNotificationService.cs`

APNs implementation. Same `_initialized` guard pattern as Android.

#### `Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken cancellationToken = default)`

On first call subscribes to the three `ApplePushBridge` events. Always dispatches
`UIApplication.SharedApplication.RegisterForRemoteNotifications()` onto the main thread via
`MainThread.BeginInvokeOnMainThread`, then logs environment and topic. Returns `true`.

#### `Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default)`

Returns `ApplePushBridge.DeviceToken` when present. Otherwise re-triggers
`RegisterForRemoteNotifications` and returns the bridge value, which may still be `null`
until the `AppDelegate` callback fires.

#### `Task SubscribeToTopicAsync(...)` / `Task UnsubscribeFromTopicAsync(...)`

Log the topic change and complete. **APNs has no server-side topic subscription API**, so
topic opt-in is enforced by the backend when it builds the payload (using a custom `topic`
key) and locally by the preferences service. These methods are the extension point for a
real backend call.

#### `Task DeleteTokenAsync(CancellationToken cancellationToken = default)`

Dispatches `UIApplication.SharedApplication.UnregisterForRemoteNotifications()` on the main
thread.

### 6.6 `UnsupportedPushNotificationService`

`Services/Notifications/UnsupportedPushNotificationService.cs`

Desktop fallback registered for Windows and Mac Catalyst. `InitializeAsync` returns `false`
and `GetDeviceTokenAsync` returns `null`; topic and token methods complete immediately. The
three events are declared with `#pragma warning disable CS0067` because the interface
requires them but this target never raises them.

### 6.7 `INotificationPermissionService`

`Services/Notifications/INotificationPermissionService.cs`

| Member | Signature |
| --- | --- |
| `GetStatusAsync` | `Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default)` |
| `RequestAsync` | `Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default)` |

`GetStatusAsync` never prompts; `RequestAsync` prompts when the platform allows it.

### 6.8 `AndroidNotificationPermissionService`

`Platforms/Android/AndroidNotificationPermissionService.cs`

Uses a static `TaskCompletionSource<NotificationPermissionStatus>` to bridge the
fire-and-forget Android permission dialog back to an awaitable task.

#### `Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default)`

Below API 33 returns `Granted` (notifications are implicitly allowed). Otherwise checks
`ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications)` and
returns `Granted` or `Denied`.

#### `Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default)`

Below API 33 returns `Granted`. Otherwise:

1. Reads `Platform.CurrentActivity`; returns `Unknown` when there is no activity.
2. Creates a new `TaskCompletionSource` with `RunContinuationsAsynchronously` and stores it
   in the static field.
3. Calls `ActivityCompat.RequestPermissions(activity, [POST_NOTIFICATIONS], RequestCode)`.
4. Returns the pending task.

Request code is `0x5100`.

#### `internal static void HandlePermissionResult(string[] permissions, Permission[] grantResults)`

Called by `MainActivity.OnRequestPermissionsResult`. Atomically exchanges the pending
source for `null` with `Interlocked.Exchange` (so a duplicate callback is ignored) and
completes it with `Granted` when the first grant result is `Permission.Granted`, otherwise
`Denied`.

### 6.9 `AppleNotificationPermissionService`

`Platforms/iOS/AppleNotificationPermissionService.cs`

#### `Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default)`

Awaits `UNUserNotificationCenter.Current.GetNotificationSettingsAsync()` and maps
`AuthorizationStatus`.

#### `Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default)`

Requests `Alert | Badge | Sound`. Returns `Denied` when not approved, otherwise re-reads the
status through `GetStatusAsync` so the caller sees the authoritative value.

#### `private static NotificationPermissionStatus Map(UNAuthorizationStatus status)`

`Authorized`, `Provisional` and `Ephemeral` map to `Granted`; `Denied` to `Denied`;
`NotDetermined` to `NotRequested`; anything else to `Unknown`.

### 6.10 `AlwaysGrantedNotificationPermissionService`

`Services/Notifications/AlwaysGrantedNotificationPermissionService.cs`

Desktop fallback. Both methods return `Granted`.

### 6.11 `INotificationPreferencesService` / `NotificationPreferencesService`

`Services/Preferences/INotificationPreferencesService.cs`,
`Services/Preferences/NotificationPreferencesService.cs`

Owns the user's topic opt-ins. State lives in `IPreferences` under the key
`pn.topic.{Topic}`. Each change is mirrored to the push provider and the backend.

The catalog is a static tuple array that pairs each topic with its display name and
description, and drives the preference list order:

| Topic | Display name | Description |
| --- | --- | --- |
| `Announcements` | Product announcements | News about releases and major changes. |
| `AccountAlerts` | Account alerts | Security, sign-in and billing alerts. |
| `OrderUpdates` | Order updates | Shipping and delivery status changes. |
| `Promotions` | Promotions | Offers, discounts and limited-time deals. |
| `Tips` | Tips and guides | How-to content and feature highlights. |
| `WeeklyDigest` | Weekly digest | A single summary email-style push each week. |

#### `Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(CancellationToken cancellationToken = default)`

Throws on cancellation, then projects the catalog into `NotificationPreference` objects
whose `IsEnabled` comes from `IsEnabled(topic)`. Returns a completed task.

#### `bool IsEnabled(NotificationTopic topic)`

`_preferences.Get(PreferenceKey(topic), DefaultEnabled(topic))`. The default is `true` for
`AccountAlerts` and `OrderUpdates`, `false` for everything else, so transactional topics
start enabled.

#### `Task<IReadOnlyList<NotificationPreference>> SetEnabledAsync(NotificationTopic topic, bool enabled, CancellationToken cancellationToken = default)`

Writes the preference, calls `ApplySubscriptionAsync`, calls `PersistToBackendAsync`, then
returns the refreshed list.

#### `Task<IReadOnlyList<NotificationPreference>> SetAllAsync(bool enabled, CancellationToken cancellationToken = default)`

Writes every topic, awaits `ApplySubscriptionAsync` for each, syncs to the backend once, and
returns the refreshed list.

#### `IReadOnlyCollection<NotificationTopic> GetEnabledTopics()`

Returns the enabled topics as an array. Used to populate
`DeviceRegistrationRequest.Topics`.

#### `Task SynchronizeSubscriptionsAsync(CancellationToken cancellationToken = default)`

Re-applies every stored subscription to the provider. Called after a successful
registration so the provider state matches the stored preferences.

#### `private async Task ApplySubscriptionAsync(NotificationTopic topic, bool enabled, CancellationToken cancellationToken)`

Calls subscribe or unsubscribe on `IPushNotificationService`. A failure is caught and
logged as a warning with the comment that it must not block saving the user's choice
locally.

#### `private async Task PersistToBackendAsync(CancellationToken cancellationToken)`

Resolves the installation id and calls
`SaveNotificationPreferencesAsync(installationId, GetEnabledTopics(), cancellationToken)`.
Failures are logged and swallowed.

#### `private static string PreferenceKey(NotificationTopic topic)`

Returns `$"pn.topic.{topic}"`.

#### `private static bool DefaultEnabled(NotificationTopic topic)`

`true` for `AccountAlerts` and `OrderUpdates`; `false` otherwise.

### 6.12 `IPushRegistrationCoordinator` / `PushRegistrationCoordinator`

`Services/PushRegistrationCoordinator.cs`

Orchestrates the whole push lifecycle.

| Member | Signature |
| --- | --- |
| `CurrentDeviceToken` | `string?` — current token, or `null` |
| `CurrentInstallation` | `DeviceInstallation?` — latest snapshot |
| `RegisterAsync` | `Task<DeviceRegistrationResult> RegisterAsync(bool requestPermission = true, CancellationToken cancellationToken = default)` |
| `SaveInstallationAsync` | `Task<DeviceRegistrationResult> SaveInstallationAsync(CancellationToken cancellationToken = default)` |
| `UnregisterAsync` | `Task UnregisterAsync(CancellationToken cancellationToken = default)` |

The constructor stores its dependencies, binds `PushOptions` via `IOptions<PushOptions>`,
and subscribes to `IPushNotificationService.TokenRefreshed`.

#### `Task<DeviceRegistrationResult> RegisterAsync(bool requestPermission = true, CancellationToken cancellationToken = default)`

1. Obtain permission: `RequestAsync` when `requestPermission` is true, otherwise
   `GetStatusAsync`.
2. When the status is not `Granted`, log and return
   `Fail($"Notification permission is {permission}.")`.
3. `await _pushNotificationService.InitializeAsync(BuildPushConfiguration(), ...)`.
4. Read the token with `GetDeviceTokenAsync`. When blank, log and return
   `Fail("Device token is not available yet.")` — a retry is expected after the token
   callback fires.
5. Cache the token in `CurrentDeviceToken`.
6. Delegate to `RegisterWithBackendAsync`.

#### `Task<DeviceRegistrationResult> SaveInstallationAsync(CancellationToken cancellationToken = default)`

Builds a fresh snapshot, attaches `CurrentDeviceToken`, caches it in `CurrentInstallation`,
and calls `SaveInstallationAsync` on the registration service.

#### `Task UnregisterAsync(CancellationToken cancellationToken = default)`

Resolves the installation id, calls `UnregisterDeviceAsync`, then
`DeleteTokenAsync` on the push service, and clears `CurrentDeviceToken`.

#### `private async Task<DeviceRegistrationResult> RegisterWithBackendAsync(string token, CancellationToken cancellationToken)`

1. Builds the installation snapshot with the token.
2. Builds a `DeviceRegistrationRequest` populated with installation id, token, platform,
   app version and the enabled topics from `GetEnabledTopics()`.
3. Calls `RegisterDeviceAsync`.
4. Calls `SaveInstallationAsync` to persist the richer record alongside the token
   registration.
5. Calls `SynchronizeSubscriptionsAsync` so provider subscriptions match local preferences.
6. Returns the registration result.

#### `private PushConfiguration BuildPushConfiguration()`

Copies the `PushOptions` values into a `PushConfiguration`.

#### `private async void OnTokenRefreshed(object? sender, string token)`

Fires when the provider rotates the token. Caches the new token and re-runs
`RegisterWithBackendAsync` so the backend always holds a live token. Failures are logged as
warnings. This is an `async void` event handler; exceptions are caught inside the method.

### 6.13 `IPushPermissionFlowService` / `PushPermissionFlowService`

`Services/PushPermissionFlowService.cs`. Orchestrates the guided permission experience so
the OS dialog is only shown after the user has agreed in the app.

- `record PushPermissionFlowResult(PushPermissionOutcome Outcome, NotificationPermissionStatus PermissionStatus, DeviceRegistrationResult? RegistrationResult)`
  — the outcome of a run. `IsGranted` is `true` when the outcome is
  `PushPermissionOutcome.OsPermissionGranted`.
- `Task<PushPermissionFlowResult> RequestPermissionWithSoftPromptAsync(bool showPreferencesOnGrant = true, CancellationToken cancellationToken = default)`
  — the single entry point. It:
  1. Resolves the visible page. When there is none it returns
     `PushPermissionOutcome.Unavailable` without prompting.
  2. Shows the in-app soft prompt (`DisplayAlertAsync` with "Continue" / "Not now"). A
     decline or dismissal returns `PushPermissionOutcome.SoftPromptDeclined` and the OS is
     never asked.
  3. Calls `INotificationPermissionService.RequestAsync()`, which shows the operating system
     dialog. A non-granted status returns `PushPermissionOutcome.OsPermissionDenied`.
  4. On a grant, calls `IPushRegistrationCoordinator.RegisterAsync(requestPermission: false)`
     to obtain the token and register the device, then, when `showPreferencesOnGrant` is
     `true`, calls `INotificationPreferencesDialogService.ShowAsync()` to open the preference
     popup.
  5. Returns `PushPermissionOutcome.OsPermissionGranted` with the permission status and the
     registration result.

The service keeps the OS dialog behind an explicit user agreement, which is the behavior
both Apple and Google recommend. It also means a user who declines the soft prompt is not
counted as an OS-level denial, so the OS dialog can still be shown later.

### 6.14 `INotificationPreferencesDialogService` / `NotificationPreferencesDialogService`

`Views/NotificationPreferencesDialogService.cs`. Presents the notification preference view
as a popup.

- `Task ShowAsync(CancellationToken cancellationToken = default)` — resolves the visible
  page, creates `NotificationPreferencesPopupPage` with the shared
  `NotificationPreferencesViewModel`, and pushes it with `PushModalAsync`. The task
  completes when the modal page is presented; the popup stays until the user closes it.

---

## 7. Platform integration

### 7.1 Android

#### `AndroidManifest.xml`

```xml
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.WAKE_LOCK" />
<uses-permission android:name="com.google.android.c2dm.permission.RECEIVE" />
```

`POST_NOTIFICATIONS` is required from API 33. `WAKE_LOCK` and the C2DM receive permission
let FCM wake the app for data messages.

#### `MainApplication`

Standard MAUI `[Application]` entry point calling `MauiProgram.CreateMauiApp()`.

#### `MainActivity`

`[Activity(..., MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ...)]`. Single-top
launch mode means a notification tap reuses the existing activity and arrives through
`OnNewIntent` instead of creating a new one.

- `OnCreate(Bundle? savedInstanceState)` — calls base then `HandleNotificationIntent(Intent)`
  so a cold start from a tap is handled.
- `OnNewIntent(Intent? intent)` — calls base then `HandleNotificationIntent(intent)` for the
  warm path.
- `OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)`
  — forwards to `AndroidNotificationPermissionService.HandlePermissionResult` before calling
  base, completing the awaited permission task.
- `private static void HandleNotificationIntent(Intent? intent)` — returns early when the
  intent has no extras or lacks the `google.message_id` extra that FCM adds to
  notification-tap launches. Otherwise maps the intent with
  `FirebaseMessageMapper.MapIntent` and raises it on `FirebasePushBridge`.

#### `AppFirebaseMessagingService`

`[Service(Exported = false)]` with an intent filter for `com.google.firebase.MESSAGING_EVENT`.
The Android framework instantiates this type, so it has no constructor injection and
forwards everything through `FirebasePushBridge`.

- `OnNewToken(string token)` — calls base and raises
  `FirebasePushBridge.RaiseTokenRefreshed(token)`. Wrapped in
  `#pragma warning disable CS0618, CS0672` because the binding marks the base member
  obsolete; it remains the supported token-rotation callback.
- `OnMessageReceived(RemoteMessage message)` — calls base, then raises
  `RaiseMessageReceived(FirebaseMessageMapper.Map(message, IsAppInForeground()))`.
- `private static bool IsAppInForeground()` — reads `ActivityManager` from the application
  context, finds the `RunningAppProcesses` entry whose `ProcessName` equals the package
  name, and returns whether its `Importance` is `Foreground`.

#### `FirebasePushBridge`

`internal static` class. Holds three static events (`TokenRefreshed`, `MessageReceived`,
`NotificationTapped`) and the matching `Raise*` methods that invoke them with a `null`
sender. This is the seam between OS-instantiated components and the DI singleton.

#### `FirebaseMessageMapper`

`internal static` class with two mapping methods and a private `Get` helper.

- `static PushMessage Map(RemoteMessage message, bool wasForeground)` — copies
  `message.Data` into a dictionary, then resolves title and body from
  `message.GetNotification()` first and falls back to the `title`/`body` data keys.
  `Topic` is `message.From`; `MessageId` is `message.MessageId`.
- `static PushMessage MapIntent(Intent intent)` — reads the extras key set into a
  dictionary, then resolves `MessageId` from `google.message_id`, title from `gcm.n.title`
  or `title`, body from `gcm.n.body` or `body`, and topic from `from`. `WasForeground` is
  `false` because the user came from a notification.
- `private static string? Get(IReadOnlyDictionary<string, string> data, string key)` —
  `TryGetValue` wrapper.

#### `DeviceInfoProvider.Platform.cs` (Android)

See section 6.1.

#### `google-services.json.template`

A reference structure showing the shape Firebase expects (`project_info`, `client` with
`client_info` and `api_key`). Copy the real file from the Firebase console to
`Platforms/Android/google-services.json`.

### 7.2 iOS

#### `AppDelegate`

`[Register("AppDelegate")]`, derives from `MauiUIApplicationDelegate`.

- `CreateMauiApp()` — returns `MauiProgram.CreateMauiApp()`.
- `FinishedLaunching(UIApplication, NSDictionary)` — installs a single
  `AppUserNotificationCenterDelegate` instance on `UNUserNotificationCenter.Current.Delegate`
  and calls base. A single consolidated delegate avoids the duplicate-delegate problem that
  arises when the scene delegate also assigns one.
- `[Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]`
  `RegisteredForRemoteNotifications(UIApplication, NSData deviceToken)` — converts
  `deviceToken.Description` by trimming the angle brackets and removing spaces to produce
  the hex token, then calls `ApplePushBridge.SetDeviceToken`.
- `[Export("application:didFailToRegisterForRemoteNotificationsWithError:")]`
  `FailedToRegisterForRemoteNotifications(UIApplication, NSError error)` — writes the
  localized description to `Debug` output and continues without a token.
- `[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]`
  `ReceivedRemoteNotification(UIApplication, NSDictionary userInfo, Action<UIBackgroundFetchResult>)`
  — raises `RaiseMessageReceived` with `WasForeground` set from the application state, then
  calls the completion handler with `NewData`.

#### `AppUserNotificationCenterDelegate`

`UNUserNotificationCenterDelegate` subclass.

- `WillPresentNotification(UNUserNotificationCenter, UNNotification, Action<UNNotificationPresentationOptions>)`
  — builds a `PushMessage` from the notification content with `WasForeground = true` and
  raises it, then completes with `Banner | Sound` so the notification is visible while the
  app is open.
- `DidReceiveNotificationResponse(UNUserNotificationCenter, UNNotificationResponse, Action)`
  — builds a `PushMessage` from the tapped notification with `WasForeground = false` and
  raises `RaiseNotificationTapped`, then calls the completion handler.

#### `NSDictionaryExtensions.ToDictionary()`

`internal static` extension converting an `NSDictionary?` into
`Dictionary<string, string>` by stringifying each key and value. Returns an empty dictionary
for `null`.

#### `ApplePushBridge`

`internal static` class. Unlike the Android bridge it also stores state: `_deviceToken`
guarded by a `Sync` lock, exposed through the `DeviceToken` property. `SetDeviceToken`
stores the token under the lock and then raises `TokenRefreshed`. `RaiseMessageReceived` and
`RaiseNotificationTapped` fan out the other two events.

#### `Info.plist`

Adds:

```xml
<key>UIBackgroundModes</key>
<array>
  <string>remote-notification</string>
</array>
```

This allows silent/background pushes to wake the app for
`didReceiveRemoteNotification:fetchCompletionHandler:`. The rest of the file is the standard
MAUI template (device family, orientations, scene manifest, app icon assets).

#### `SceneDelegate`

Standard MAUI `MauiUISceneDelegate`. Deliberately does not set
`UNUserNotificationCenter.Current.Delegate`; `AppDelegate` owns that.

### 7.3 Windows and Mac Catalyst

Both targets register `UnsupportedPushNotificationService` and
`AlwaysGrantedNotificationPermissionService` and provide a `DeviceInfoProvider.Platform.cs`
partial. This keeps the shared UI functional so the sample runs and the preferences and
device screens can be exercised without a push provider.

Mac Catalyst's `Entitlements.plist` enables the App Sandbox and outgoing network client,
which is the template default.

---

## 8. View models

### 8.1 `BaseViewModel`

`ViewModels/BaseViewModel.cs`. Minimal `INotifyPropertyChanged` base.

- `event PropertyChangedEventHandler? PropertyChanged`
- `bool IsBusy` — busy flag used to disable re-entrant commands.
- `string StatusMessage` — status line shown by the pages.
- `protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)`
  — returns `false` when the value is unchanged, otherwise assigns, raises
  `PropertyChanged` and returns `true`. The boolean return is what lets a setter run a side
  effect only on a real change.
- `protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)`

### 8.2 `NotificationPreferenceItem`

`ViewModels/NotificationPreferenceItem.cs`. One row in the preference list.

- Constructor
  `NotificationPreferenceItem(NotificationPreference preference, Action<NotificationPreferenceItem> onToggled)`
  — copies `Topic`, `DisplayName`, `Description` and the initial `IsEnabled`, and stores the
  toggle callback.
- `NotificationTopic Topic`, `string DisplayName`, `string Description` — get-only.
- `bool IsEnabled` — setter calls `SetProperty`; when it returns `true` it invokes the
  toggle callback so the view model can persist the change.
- `void SetEnabledSilently(bool enabled)` — updates the backing field and raises
  `PropertyChanged` without invoking the callback. Used when the view model itself updates
  the rows, to avoid a feedback loop.

### 8.3 `NotificationPreferencesViewModel`

`ViewModels/NotificationPreferencesViewModel.cs`. Backs the preferences page.

- `ObservableCollection<NotificationPreferenceItem> Preferences`
- `ICommand ToggleAllCommand` — `Command<bool>` calling `ToggleAllAsync`.
- `bool AllEnabled` — master switch. The setter calls `SetProperty` and, on a real change,
  fires `ToggleAllAsync(value)`.
- `Task LoadAsync(CancellationToken cancellationToken = default)` — returns early when
  busy; loads preferences, clears and repopulates `Preferences`, computes `AllEnabled`,
  raises `PropertyChanged` for it, and sets a status such as
  `"3 of 6 topics enabled."`.
- `private async Task ToggleAllAsync(bool enabled)` — calls `SetAllAsync`, applies the
  resulting state to every row with `SetEnabledSilently` (matching by topic), and sets the
  status. Exceptions are logged and surfaced as
  `"Could not update preferences."`.
- `private async void OnItemToggled(NotificationPreferenceItem item)` — persists a single
  topic via `SetEnabledAsync`, recomputes `AllEnabled`, and updates the status. On failure
  it reverts the row with `SetEnabledSilently(!item.IsEnabled)`.

### 8.4 `DeviceInfoViewModel` and `DeviceInfoRow`

`ViewModels/DeviceInfoViewModel.cs`.

- `ObservableCollection<DeviceInfoRow> Rows`
- `ICommand RegisterCommand`, `SaveInstallationCommand`, `UnregisterCommand`
- `string PermissionStatus` — private setter, updated from the permission service.
- `Task LoadAsync()` — builds the snapshot from `IDeviceInfoProvider`, attaches
  `IPushRegistrationCoordinator.CurrentDeviceToken`, populates the rows, and reads the
  permission status. Errors are logged and surfaced as
  `"Could not load device information."`.
- `private async Task RegisterAsync()` — guards on `IsBusy`, calls
  `IPushRegistrationCoordinator.RegisterAsync()`, reports the result, and reloads.
- `private async Task SaveInstallationAsync()` — calls
  `IPushRegistrationCoordinator.SaveInstallationAsync()` and reloads.
- `private async Task UnregisterAsync()` — calls
  `IPushRegistrationCoordinator.UnregisterAsync()` and reloads.
- `private void PopulateRows(DeviceInstallation installation)` — clears `Rows` and adds
  fifteen label/value pairs: Installation Id, Device Token (or `(not available yet)`),
  Platform, Model, Manufacturer, Device Name, OS Version, App Version (version + build),
  Bundle Id, Locale, Language, Time Zone, Screen, Device Type and Emulator.
- `private void Add(string label, string value)` — appends a `DeviceInfoRow`.
- `public sealed record DeviceInfoRow(string Label, string Value)` — the row type.

### 8.5 `HomeViewModel`

`ViewModels/HomeViewModel.cs`.

- Subscribes to `MessageReceived` and `NotificationTapped` in the constructor.
- `ObservableCollection<PushMessage> ReceivedMessages`
- `ICommand RequestPermissionCommand`, `RefreshCommand`
- `string PermissionStatus`, `string DeviceToken` — private setters.
- `Task LoadAsync()` — delegates to `RefreshAsync`.
- `private async Task RequestPermissionAsync()` — guards on `IsBusy`, calls
  `IPushPermissionFlowService.RequestPermissionWithSoftPromptAsync()` (which shows the soft
  prompt, then the OS dialog, then the preference popup), maps the outcome to a status
  message, and refreshes.
- `private async Task RefreshAsync()` — reads the permission status and resolves the token
  from `CurrentDeviceToken`, falling back to `IPushNotificationService.GetDeviceTokenAsync()`,
  showing `"Not available"` when empty.
- `private void OnMessageReceived(object?, PushMessage)` — marshals to the main thread,
  inserts the message at the top of the list and updates the status.
- `private void OnNotificationTapped(object?, PushMessage)` — same, with a status noting the
  app was opened from a notification.

Both handlers use `MainThread.BeginInvokeOnMainThread` because push callbacks can arrive on
background threads and `ObservableCollection` mutation must happen on the UI thread.

---

## 9. Views

### 9.1 `AppShell.xaml`

A `TabBar` with three `ShellContent` entries, each using a `DataTemplate` so the page is
resolved from DI. `Title="Push Notification Sample"`.

### 9.2 `HomePage.xaml`

`x:DataType="vm:HomeViewModel"`. A two-row grid:

- Header stack: title, a bordered card showing `PermissionStatus` and `DeviceToken`
  (truncated to two lines), an "Enable notifications" button bound to
  `RequestPermissionCommand` (which starts the guided soft prompt flow), and a
  "Refresh status" button bound to `RefreshCommand`.
- A `CollectionView` bound to `ReceivedMessages` with an empty view reading
  "No messages received yet." Each item shows title, body and a `ReceivedAtUtc` timestamp.

Code-behind resolves `HomeViewModel` from DI, sets `BindingContext`, and calls
`LoadAsync()` from `OnAppearing`.

### 9.3 `NotificationPreferencesView.xaml`

`x:DataType="vm:NotificationPreferencesViewModel"`. The single definition of the preference
UI, shared by the Preferences tab and the post-permission popup. It is a `ContentView` with
a code-behind constructor that takes the view model, because XAML cannot resolve a
constructor-injected view model. Three rows:

- Header with title, explanatory subtitle and a Close button. The button is hidden by
  default and shown by setting `ShowCloseButton`, which the popup does.
- A `ScrollView` containing a master-switch card (bound to `AllEnabled`) and a
  `CollectionView` of `NotificationPreferenceItem` rows, each with a `Switch` bound to
  `IsEnabled`.
- Footer with an `ActivityIndicator` bound to `IsBusy` and a `StatusMessage` label.

The view raises `CloseRequested` when the Close button is tapped and exposes `LoadAsync()`,
which the hosts call from `OnAppearing`.

### 9.4 `NotificationPreferencesPage.xaml`

The Preferences tab. A thin `ContentPage` whose `Host` `ContentView` holds a
`NotificationPreferencesView` created in the code-behind with the injected view model. It
calls `LoadAsync()` from `OnAppearing`.

### 9.5 `NotificationPreferencesPopupPage.xaml` and `NotificationPreferencesDialogService`

`NotificationPreferencesPopupPage` is the popup shown after the user allows the OS
permission. It is a modal `ContentPage` with a transparent background, a scrim `BoxView`
that dismisses the popup when tapped, and a rounded `Card` `Border` that hosts the same
`NotificationPreferencesView` (with `ShowCloseButton = true`). The card is capped at
520 x 640 so it reads as a dialog on tablets.

`NotificationPreferencesDialogService` implements `INotificationPreferencesDialogService`.
`ShowAsync` resolves the visible page, creates the popup with the shared view model and
pushes it with `PushModalAsync`. Keeping this behind an interface means
`PushPermissionFlowService` does not touch the navigation stack directly.

### 9.6 `DeviceInfoPage.xaml`

`x:DataType="vm:DeviceInfoViewModel"`. Three rows:

- Header with title, explanation and the current `PermissionStatus`.
- A `CollectionView` of `DeviceInfoRow` items in a two-column grid (140 px label column).
- Footer with `IsBusy`, `StatusMessage` and a three-button grid: Register, Save and
  Unregister, bound to the corresponding commands.

All pages call the view model's `LoadAsync` from `OnAppearing`, so navigating to a tab
refreshes its data.

---

## 10. End-to-end processes

### 10.1 Application startup

```
MAUI host
  -> MauiProgram.CreateMauiApp()
       -> load embedded appsettings.json
       -> add PUSH_ environment variables
       -> Configure<PushOptions>
       -> RegisterPlatformServices (push + permission for the target)
       -> RegisterSharedServices (device info, registration, preferences, coordinator, pages)
  -> App(IServiceProvider)
       -> CreateWindow resolves AppShell from DI
  -> Shell renders the Home tab
       -> HomePage.OnAppearing -> HomeViewModel.LoadAsync
            -> read permission status
            -> resolve current token (may be null)
```

No push provider is initialized at startup. Initialization happens on the first
`RegisterAsync`, which keeps the permission prompt tied to a user action.

### 10.2 Guided permission request and device registration

```
User taps "Enable notifications" (HomePage)
  -> HomeViewModel.RequestPermissionAsync
       -> PushPermissionFlowService.RequestPermissionWithSoftPromptAsync(showPreferencesOnGrant: true)
            1. resolve the visible page; none -> PushPermissionOutcome.Unavailable
            2. soft prompt: Page.DisplayAlertAsync("Turn on notifications?", ..., "Continue", "Not now")
                 "Not now" / dismiss -> PushPermissionOutcome.SoftPromptDeclined (OS never asked)
            3. user agreed -> INotificationPermissionService.RequestAsync
                 Android: Platform.CurrentActivity + ActivityCompat.RequestPermissions
                          -> MainActivity.OnRequestPermissionsResult
                          -> AndroidNotificationPermissionService.HandlePermissionResult
                 iOS:     UNUserNotificationCenter.RequestAuthorizationAsync(Alert|Badge|Sound)
            4. status != Granted -> PushPermissionOutcome.OsPermissionDenied
            5. status == Granted -> IPushRegistrationCoordinator.RegisterAsync(requestPermission: false)
                 a. IPushNotificationService.InitializeAsync(BuildPushConfiguration())
                      Android: attach bridge handlers, create notification channel
                      iOS:     attach bridge handlers, RegisterForRemoteNotifications
                 b. IPushNotificationService.GetDeviceTokenAsync()
                      Android: FirebaseMessaging.Instance.GetToken()
                      iOS:     ApplePushBridge.DeviceToken (may be null on first pass)
                 c. token blank -> Fail("Device token is not available yet.")
                 d. RegisterWithBackendAsync(token)
                      i.   IDeviceInfoProvider.GetDeviceInfo()
                      ii.  build DeviceRegistrationRequest (id, token, platform, app version, topics)
                      iii. IDeviceRegistrationService.RegisterDeviceAsync      -> POST /api/devices/register
                      iv.  IDeviceRegistrationService.SaveInstallationAsync    -> PUT  /api/installations/{id}
                      v.   INotificationPreferencesService.SynchronizeSubscriptionsAsync()
            6. INotificationPreferencesDialogService.ShowAsync()
                 -> PushModalAsync(NotificationPreferencesPopupPage with the shared view model)
       -> HomeViewModel maps the outcome to a status message and refreshes the token display
```

Because the flow calls `RegisterAsync(requestPermission: false)`, the coordinator reads the
permission that was just granted rather than prompting a second time. On iOS the first pass
often returns `Fail("Device token is not available yet.")` because the APNs token arrives
asynchronously. The token callback then drives the retry described in section 10.3, which
performs the registration without a further prompt. The preference popup is shown
regardless, so the user can choose topics as soon as permission is granted.

### 10.3 Token refresh

```
Android: FCM rotates the token
  -> AppFirebaseMessagingService.OnNewToken
       -> FirebasePushBridge.RaiseTokenRefreshed
  -> AndroidPushNotificationService.OnTokenRefreshed
       -> IPushNotificationService.TokenRefreshed event

iOS: APNs issues the token
  -> AppDelegate.RegisteredForRemoteNotifications
       -> ApplePushBridge.SetDeviceToken
            -> ApplePushBridge.TokenRefreshed
  -> ApplePushNotificationService.OnTokenRefreshed
       -> IPushNotificationService.TokenRefreshed event

Both paths converge:
  -> PushRegistrationCoordinator.OnTokenRefreshed
       -> cache CurrentDeviceToken
       -> RegisterWithBackendAsync(token)  (no permission prompt)
```

### 10.4 Foreground message receipt

```
Android: FCM delivers a message
  -> AppFirebaseMessagingService.OnMessageReceived
       -> FirebaseMessageMapper.Map(message, IsAppInForeground())
       -> FirebasePushBridge.RaiseMessageReceived
  -> AndroidPushNotificationService.MessageReceived
  -> HomeViewModel.OnMessageReceived
       -> MainThread: insert at top of ReceivedMessages, update status

iOS: notification arrives while the app is open
  -> AppUserNotificationCenterDelegate.WillPresentNotification
       -> ApplePushBridge.RaiseMessageReceived (WasForeground = true)
  -> ApplePushNotificationService.MessageReceived
  -> HomeViewModel.OnMessageReceived
```

Background data pushes on iOS additionally reach
`AppDelegate.ReceivedRemoteNotification`, which raises the same event with `WasForeground`
derived from the application state.

### 10.5 Notification tap

```
Android (warm start, SingleTop)
  -> MainActivity.OnNewIntent
       -> HandleNotificationIntent: require the google.message_id extra
       -> FirebaseMessageMapper.MapIntent(intent)
       -> FirebasePushBridge.RaiseNotificationTapped
  -> AndroidPushNotificationService.NotificationTapped
  -> HomeViewModel.OnNotificationTapped

Android (cold start)
  -> MainActivity.OnCreate -> HandleNotificationIntent(Intent)

iOS
  -> AppUserNotificationCenterDelegate.DidReceiveNotificationResponse
       -> ApplePushBridge.RaiseNotificationTapped (WasForeground = false)
  -> ApplePushNotificationService.NotificationTapped
  -> HomeViewModel.OnNotificationTapped
```

### 10.6 Changing a notification preference

```
User toggles a row switch
  -> NotificationPreferenceItem.IsEnabled setter
       -> SetProperty returns true
       -> onToggled callback
  -> NotificationPreferencesViewModel.OnItemToggled
       1. INotificationPreferencesService.SetEnabledAsync(topic, enabled)
            a. IPreferences.Set("pn.topic.{topic}", enabled)
            b. ApplySubscriptionAsync
                 enabled  -> IPushNotificationService.SubscribeToTopicAsync
                 disabled -> IPushNotificationService.UnsubscribeFromTopicAsync
            c. PersistToBackendAsync
                 -> IDeviceRegistrationService.SaveNotificationPreferencesAsync
       2. recompute AllEnabled, update status
       3. on failure: revert the row with SetEnabledSilently
```

The master switch follows the same path through `SetAllAsync`, which writes every topic and
syncs to the backend once.

### 10.7 Unregistering

```
User taps "Unregister" (DeviceInfoPage)
  -> DeviceInfoViewModel.UnregisterAsync
       -> PushRegistrationCoordinator.UnregisterAsync
            1. IDeviceRegistrationService.UnregisterDeviceAsync(installationId) -> DELETE
            2. IPushNotificationService.DeleteTokenAsync()
                 Android: FirebaseMessaging.Instance.DeleteToken()
                 iOS:     UnregisterForRemoteNotifications
            3. CurrentDeviceToken = null
       -> reload the device page
```

---

## 11. Configuration reference

`appsettings.json` is embedded in the assembly and read at startup. Every value can be
overridden by an environment variable prefixed with `PUSH_` using the double-underscore
section separator, for example `PUSH_Push__FcmSenderId`.

| Key | Type | Default | Description |
| --- | --- | --- | --- |
| `Push:ApiBaseUrl` | string | `https://api.example.com` | Backend base URL |
| `Push:FcmSenderId` | string | `""` | FCM sender id (project number) |
| `Push:FcmNotificationChannelId` | string | `general` | Android channel id |
| `Push:FcmNotificationChannelName` | string | `General` | Android channel display name |
| `Push:ApnsEnvironment` | string | `development` | `development` or `production` |
| `Push:ApnsTopic` | string | `com.companyname.pushnotification.app` | APNs topic (bundle id) |

Bound to `PushOptions` (`Configuration/PushOptions.cs`), whose `SectionName` constant is
`"Push"`. The coordinator copies these into `PushConfiguration` for the provider.

Because `appsettings.json` is an `EmbeddedResource`, its MSBuild logical name is prefixed
with the root namespace (`PushNotification.App.appsettings.json`). `MauiProgram` resolves it
by suffix match rather than by a hard-coded name.

---

## 12. Backend contract

`IDeviceRegistrationService` defines the four calls the app makes. The dummy implementation
logs each payload; the HTTP equivalents are:

| Method | HTTP |
| --- | --- |
| `RegisterDeviceAsync` | `POST /api/devices/register` |
| `SaveInstallationAsync` | `PUT /api/installations/{installationId}` |
| `UnregisterDeviceAsync` | `DELETE /api/installations/{installationId}` |
| `SaveNotificationPreferencesAsync` | `PUT /api/installations/{installationId}/preferences` |

### 12.1 Registration payload

```json
{
  "installationId": "3f2504e0-4f89-41d3-9a0c-0305e82c3301",
  "deviceToken": "fcm-or-apns-token",
  "platform": "Android",
  "userId": null,
  "topics": ["AccountAlerts", "OrderUpdates"],
  "appVersion": "1.0"
}
```

`platform` is serialized as a string name by the dummy service's
`JsonStringEnumConverter`. A real backend should accept the enum name or map it explicitly.

### 12.2 Installation record

```json
{
  "installationId": "3f2504e0-4f89-41d3-9a0c-0305e82c3301",
  "deviceToken": "fcm-or-apns-token",
  "platform": "Android",
  "platformName": "Android",
  "model": "Pixel 8",
  "manufacturer": "Google",
  "deviceName": "Pixel 8",
  "osVersion": "Android 15",
  "appVersion": "1.0",
  "appBuild": "1",
  "bundleId": "com.companyname.pushnotification.app",
  "language": "en",
  "timeZone": "America/New_York",
  "locale": "en-US",
  "screenResolution": "1080x2400",
  "screenDensity": 2.75,
  "deviceType": "Phone",
  "isEmulator": false,
  "capturedAtUtc": "2026-11-03T15:04:05.1234567+00:00"
}
```

### 12.3 Preferences payload

```json
{
  "topics": ["AccountAlerts", "OrderUpdates", "Promotions"]
}
```

### 12.4 Topic names

The canonical names, used both for FCM topics (lowercased) and backend payloads:

| Enum | Backend name | FCM topic |
| --- | --- | --- |
| `Announcements` | `Announcements` | `announcements` |
| `AccountAlerts` | `AccountAlerts` | `accountalerts` |
| `OrderUpdates` | `OrderUpdates` | `orderupdates` |
| `Promotions` | `Promotions` | `promotions` |
| `Tips` | `Tips` | `tips` |
| `WeeklyDigest` | `WeeklyDigest` | `weeklydigest` |

---

## 13. Replacing the dummy services

Two seams cover the whole backend surface.

### 13.1 Real HTTP registration client

Implement `IDeviceRegistrationService` with `HttpClient` and register it instead of the
dummy:

```csharp
services.AddSingleton<IDeviceRegistrationService, HttpDeviceRegistrationService>();
```

Keep the same result semantics: return `DeviceRegistrationResult.Ok(...)` on success and
`DeviceRegistrationResult.Fail(...)` for expected failures such as an invalid token. Throw
only for genuinely exceptional conditions, since the coordinator and view models treat a
`Fail` result as a normal outcome.

### 13.2 Real topic subscriptions on iOS

`ApplePushNotificationService.SubscribeToTopicAsync` and `UnsubscribeFromTopicAsync` are
where a backend call belongs. Have the backend store the subscription against the
installation and apply the `topic` custom key when building APNs payloads.

### 13.3 Wiring a real provider on desktop

Replace `UnsupportedPushNotificationService` with a provider implementation (for example
Windows App SDK notifications) and register it in the `#else` branch of
`RegisterPlatformServices`.

### 13.4 Real authentication

`DeviceRegistrationRequest.UserId` is the place to attach the signed-in user. Populate it in
`PushRegistrationCoordinator.RegisterWithBackendAsync` from your identity service.

---

## 14. Build, run and troubleshooting

### 14.1 Build commands

```bash
# Android (works on any host with the android workload)
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-android

# iOS (macOS only)
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-ios

# Windows
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-windows10.0.19041.0
```

The verified configuration is Android Debug with 0 warnings and 0 errors.

### 14.2 Android setup

1. Create a Firebase project and add an Android app with package name
   `com.companyname.pushnotification.app` (or change `ApplicationId` in the `.csproj`).
2. Download `google-services.json` to
   `src/PushNotification.App/Platforms/Android/google-services.json`.
3. Optionally set `Push:FcmSenderId` from the project number.

`google-services.json` is gitignored because it contains an API key.

### 14.3 iOS setup

1. Create an APNs key or certificate and enable Push Notifications for the App ID.
2. Add the Push Notifications capability (an `aps-environment` entitlement) to the app.
3. Set `Push:ApnsTopic` to the bundle identifier.
4. Test on a physical device; the simulator cannot receive pushes.

### 14.4 Troubleshooting

| Symptom | Likely cause | Action |
| --- | --- | --- |
| `error XAAMM0000: uses-sdk:minSdkVersion 21 cannot be smaller than version 23` | Android min SDK below the library requirement | Keep `SupportedOSPlatformVersion` for Android at 24 or higher |
| `Type androidx.fragment.app.FragmentKt is defined multiple times` | AndroidX version mismatch from Firebase | Keep the AndroidX pins in the `.csproj` aligned with the resolved versions |
| `Fail("Device token is not available yet.")` | Token not issued yet, or Firebase config missing | Add `google-services.json`, confirm network access, retry after the token callback |
| Permission always `Denied` on Android | `POST_NOTIFICATIONS` not granted | Confirm the manifest entry and that `MainActivity.OnRequestPermissionsResult` forwards to `HandlePermissionResult` |
| No messages while the app is open on iOS | Presentation options not returned | Confirm `WillPresentNotification` completes with `Banner \| Sound` |
| `Items that are outside Target elements must have one of the following operations` | `RuntimeIdentifier` placed in an `ItemGroup` | Keep it in a `PropertyGroup` |
| Preferences not reflected on the server | Dummy service only logs | Expected; see section 13.1 |

---

## 15. Known limitations

1. **Dummy backend.** No network calls are made. The registration and preferences flows
   log payloads only. Replacing the service is a single registration change.
2. **APNs topic subscriptions.** APNs has no server-side topic API, so
   `SubscribeToTopicAsync`/`UnsubscribeFromTopicAsync` only log. Topic targeting must be
   enforced by the backend.
3. **iOS not compiled here.** The build host has only the Android workload, so the iOS,
   Mac Catalyst and Windows targets are compiled only on their native hosts.
4. **`DeviceInstallation` intentionally omits an accessibility flag.** An earlier
   `AccessibilityEnabled` member relied on a `SemanticScreenReader` API that does not expose
   an `Enabled` property; it was removed rather than shipped broken.
5. **Desktop push is a no-op.** Windows and Mac Catalyst run the shared UI but do not
   receive push notifications.
6. **No automated tests.** Validation is `dotnet build`. The dummy services make it
   straightforward to add unit tests around the coordinator and preferences service by
   substituting the interfaces.
7. **Not production hardened.** This is a sample generated by an AI agent (OpenHands) on
   behalf of the repository owner. Review security, error handling and retry behaviour
   before production use.
