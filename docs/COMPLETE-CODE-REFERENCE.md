# PushNotification Sample — Complete Code Reference

An exhaustive reference for every type, member, and process in the .NET 10 MAUI push
notification sample. This document is intended to be complete: every public and private
method, every property, every XAML binding, every event handler, and every end-to-end
process is listed.

The companion document [TECHNICAL-DOCUMENTATION.md](TECHNICAL-DOCUMENTATION.md) explains the
architecture and rationale. This document is the method-level reference.

---

## Table of contents

1. [Scope and conventions](#1-scope-and-conventions)
2. [File inventory](#2-file-inventory)
3. [Project and build configuration](#3-project-and-build-configuration)
4. [Application startup and dependency injection](#4-application-startup-and-dependency-injection)
5. [Data models — complete reference](#5-data-models--complete-reference)
6. [Configuration](#6-configuration)
7. [Service reference — complete reference](#7-service-reference--complete-reference)
8. [Platform reference — complete reference](#8-platform-reference--complete-reference)
9. [View models — complete reference](#9-view-models--complete-reference)
10. [Views — complete reference](#10-views--complete-reference)
11. [End-to-end processes](#11-end-to-end-processes)
12. [Backend contract](#12-backend-contract)
13. [Extension points](#13-extension-points)
14. [Build, run and troubleshooting](#14-build-run-and-troubleshooting)
15. [Known limitations](#15-known-limitations)
16. [Appendix A — complete member index](#appendix-a--complete-member-index)
17. [Appendix B — notification topic catalog](#appendix-b--notification-topic-catalog)

---

## 1. Scope and conventions

- **Notation.** `Task<T>` return types are asynchronous. `private` members are marked. All
  other members are public unless stated otherwise.
- **Threading.** Methods that must run on the UI thread say so. The sample uses
  `MainThread.BeginInvokeOnMainThread` and `MainThread.InvokeOnMainThreadAsync` for that
  purpose.
- **Cancellation.** Methods that accept a `CancellationToken` forward it to the underlying
  operation. Where a parameter is optional it is written as `= default`.
- **Sample status.** This is a sample. Dummy services log payloads instead of making network
  calls.

---

## 2. File inventory

```
PushNotification.slnx                                   Solution (XML format)
README.md                                               Getting started
docs/TECHNICAL-DOCUMENTATION.md                         Architecture guide
docs/COMPLETE-CODE-REFERENCE.md                         This document
src/PushNotification.App/
  PushNotification.App.csproj                           Project + per-platform packages
  appsettings.json                                      Embedded push configuration
  MauiProgram.cs                                        DI composition root
  App.xaml / App.xaml.cs                                Application host
  AppShell.xaml / AppShell.xaml.cs                      Tab bar navigation
  .gitignore                                            Build outputs + google-services.json
  Configuration/
    PushOptions.cs                                      Strongly typed configuration
  Models/
    DeviceInstallation.cs                               Installation snapshot
    DeviceRegistrationRequest.cs                        Registration payload
    DeviceRegistrationResult.cs                         Backend response + factories
    NotificationPermissionStatus.cs                     Permission enum
    NotificationPreference.cs                           Topic preference value object
    NotificationTopic.cs                                Topic enum
    PlatformType.cs                                     Platform enum
    PushConfiguration.cs                                Runtime provider configuration
    PushMessage.cs                                      Normalized received message
    PushPermissionOutcome.cs                            Guided flow outcome enum
  Services/
    IDeviceInfoProvider.cs                              Device metadata contract
    DeviceInfoProvider.cs                               MAUI Essentials implementation
    PushRegistrationCoordinator.cs                      Lifecycle orchestration
    PushPermissionFlowService.cs                        Soft prompt -> OS prompt -> popup
    Api/
      IDeviceRegistrationService.cs                     Backend contract
      DummyDeviceRegistrationService.cs                 Logging implementation
    Notifications/
      IPushNotificationService.cs                       Push contract
      INotificationPermissionService.cs                 Permission contract
      UnsupportedPushNotificationService.cs             Desktop fallback (push)
      AlwaysGrantedNotificationPermissionService.cs     Desktop fallback (permission)
    Preferences/
      INotificationPreferencesService.cs                Preferences contract
      NotificationPreferencesService.cs                 Persistence + subscription sync
  ViewModels/
    BaseViewModel.cs                                    INotifyPropertyChanged base
    HomeViewModel.cs                                    Home screen logic
    NotificationPreferencesViewModel.cs                 Preferences screen logic
    NotificationPreferenceItem.cs                       One toggle row
    DeviceInfoViewModel.cs                              Device screen logic + DeviceInfoRow
  Views/
    HomePage.xaml / .xaml.cs                            Home tab
    NotificationPreferencesView.xaml / .xaml.cs         Shared preference view
    NotificationPreferencesPage.xaml / .xaml.cs         Preferences tab host
    NotificationSoftPromptPage.xaml / .xaml.cs         Customisable soft prompt popup
    NotificationPreferencesPopupPage.xaml / .xaml.cs    Preference popup
    NotificationPreferencesDialogService.cs             Popup presenter
    DeviceInfoPage.xaml / .xaml.cs                      Device tab
  Platforms/
    Android/
      MainActivity.cs                                   Permission callback + tap routing
      MainApplication.cs                                MAUI application entry
      AndroidManifest.xml                               Permissions
      AndroidPushNotificationService.cs                 FCM implementation
      AndroidNotificationPermissionService.cs           POST_NOTIFICATIONS handling
      AppFirebaseMessagingService.cs                    FCM receive callbacks
      FirebasePushBridge.cs                             OS-to-DI event fan-out
      FirebaseMessageMapper.cs                          Payload to PushMessage mapping
      DeviceInfoProvider.Platform.cs                    Model/manufacturer/emulator
      Resources/values/colors.xml                       Android colour overrides
      google-services.json.template                     Reference Firebase config
    iOS/
      AppDelegate.cs                                    APNs registration + delegate
      ApplePushNotificationService.cs                   APNs implementation
      AppleNotificationPermissionService.cs             UNUserNotificationCenter handling
      ApplePushBridge.cs                                OS-to-DI event fan-out
      DeviceInfoProvider.Platform.cs                    Model via sysctl
      Info.plist                                        UIBackgroundModes: remote-notification
      SceneDelegate.cs, Program.cs                      App entry
      Resources/PrivacyInfo.xcprivacy                   Apple privacy manifest
    MacCatalyst/
      AppDelegate.cs, Program.cs, SceneDelegate.cs      App entry
      DeviceInfoProvider.Platform.cs                    Model/manufacturer
      Info.plist, Entitlements.plist                    Sandbox + network client
    Windows/
      App.xaml / App.xaml.cs                            WinUI application
      DeviceInfoProvider.Platform.cs                    Model/manufacturer
      Package.appxmanifest, app.manifest                Packaging
  Resources/
    Styles/Colors.xaml, Styles/Styles.xaml              Theme resources
    Fonts/, Images/, AppIcon/, Splash/, Raw/            Assets
```

---

## 3. Project and build configuration

### 3.1 Target frameworks

`PushNotification.App.csproj` selects target frameworks by build host:

```xml
<TargetFrameworks>net10.0-android</TargetFrameworks>
<TargetFrameworks Condition="!$([MSBuild]::IsOSPlatform('linux'))">$(TargetFrameworks);net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
```

- Linux: Android only.
- macOS: Android, iOS, Mac Catalyst.
- Windows: Android, iOS, Mac Catalyst, Windows.

### 3.2 Key properties

| Property | Value |
| --- | --- |
| `OutputType` | `Exe` |
| `RootNamespace` | `PushNotification.App` |
| `UseMaui` / `SingleProject` | `true` |
| `ImplicitUsings` | `enable` |
| `Nullable` | `enable` |
| `MauiXamlInflator` | `SourceGen` |
| `ApplicationTitle` | `PushNotification.App` |
| `ApplicationId` | `com.companyname.pushnotification.app` |
| `ApplicationDisplayVersion` | `1.0` |
| `ApplicationVersion` | `1` |
| `WindowsPackageType` | `None` |
| `SupportedOSPlatformVersion` (android) | `24.0` |
| `SupportedOSPlatformVersion` (ios) | `15.0` |
| `SupportedOSPlatformVersion` (maccatalyst) | `17.0` |
| `SupportedOSPlatformVersion` (windows) | `10.0.17763.0` |
| `TargetPlatformMinVersion` (windows) | `10.0.17763.0` |

### 3.3 Package references

Shared:

- `Microsoft.Maui.Controls` (`$(MauiVersion)`)
- `Microsoft.Extensions.Logging.Debug` 10.0.0
- `Microsoft.Extensions.Configuration.Json` 10.0.0
- `Microsoft.Extensions.Configuration.Binder` 10.0.0
- `Microsoft.Extensions.Configuration.EnvironmentVariables` 10.0.0
- `Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.0

Android only:

- `Xamarin.Firebase.Messaging` 125.1.3
- `Xamarin.AndroidX.Fragment.Ktx` 1.9.0
- `Xamarin.AndroidX.Collection.Ktx` 1.6.0.1
- `Xamarin.AndroidX.Lifecycle.LiveData` 2.11.0.1
- `Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx` 2.11.0.1
- `Xamarin.AndroidX.Lifecycle.Process` 2.11.0.1

The AndroidX pins keep the Firebase and MAUI AndroidX graphs aligned so `d8` does not see
duplicate Java classes.

### 3.4 MSBuild items

| Item | Purpose |
| --- | --- |
| `MauiIcon` | App icon from `appicon.svg` + `appiconfg.svg`, colour `#512BD4` |
| `MauiSplashScreen` | Splash from `splash.svg`, colour `#512BD4` |
| `MauiImage` | `Resources/Images/*`; `dotnet_bot.png` resized to 300x185 |
| `MauiFont` | `Resources/Fonts/*` |
| `MauiAsset` | `Resources/Raw/**` with the `Raw/` prefix stripped |
| `EmbeddedResource` | `appsettings.json`, read at startup |
| `GoogleServicesJson` | `Platforms/Android/google-services.json` when it exists |

### 3.5 iOS runtime identifier

```xml
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios' and '$(RuntimeIdentifier)' == '' and '$(Configuration)' == 'Debug'">
    <RuntimeIdentifier>iossimulator-x64</RuntimeIdentifier>
</PropertyGroup>
```

`RuntimeIdentifier` must stay in a `PropertyGroup`; placing it in an `ItemGroup` fails the
build.

---

## 4. Application startup and dependency injection

### 4.1 `MauiProgram` (`MauiProgram.cs`)

Static class with one public method and two private helpers.

#### `public static MauiApp CreateMauiApp()`

Builds the MAUI host:

1. `MauiApp.CreateBuilder()`.
2. `builder.UseMauiApp<App>()`.
3. `builder.ConfigureFonts(...)` registers `OpenSans-Regular.ttf` as `OpenSansRegular` and
   `OpenSans-Semibold.ttf` as `OpenSansSemibold`.
4. Reads the embedded `appsettings.json`:
   - `typeof(MauiProgram).Assembly.GetManifestResourceNames()` finds the resource whose name
     ends with `appsettings.json` (case-insensitive).
   - Opens the stream and calls `builder.Configuration.AddJsonStream(stream)`.
5. `builder.Configuration.AddEnvironmentVariables("PUSH_")` so `PUSH_Push__FcmSenderId`
   overrides `Push:FcmSenderId`.
6. `builder.Services.Configure<PushOptions>(builder.Configuration.GetSection("Push"))`.
7. Calls `RegisterPlatformServices` then `RegisterSharedServices`.
8. In `DEBUG`, `builder.Logging.AddDebug()`.
9. Returns `builder.Build()`.

#### `private static void RegisterSharedServices(IServiceCollection services)`

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `IPreferences` | `Preferences.Default` (instance) | Singleton |
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

#### `private static void RegisterPlatformServices(IServiceCollection services)`

Compiled per target:

| Target | `IPushNotificationService` | `INotificationPermissionService` |
| --- | --- | --- |
| `ANDROID` | `AndroidPushNotificationService` | `AndroidNotificationPermissionService` |
| `IOS` | `ApplePushNotificationService` | `AppleNotificationPermissionService` |
| otherwise | `UnsupportedPushNotificationService` | `AlwaysGrantedNotificationPermissionService` |

### 4.2 `App` (`App.xaml`, `App.xaml.cs`)

- `App.xaml` merges `Resources/Styles/Colors.xaml` and `Resources/Styles/Styles.xaml`.
- `App(IServiceProvider services)` stores the container.
- `protected override Window CreateWindow(IActivationState?)` resolves `AppShell` from the
  container and returns `new Window(shell)`.

### 4.3 `AppShell` (`AppShell.xaml`, `AppShell.xaml.cs`)

`Shell` with a `TabBar` of three `ShellContent` entries, each using a `DataTemplate` so the
page is resolved from DI:

| Title | `ContentTemplate` | Route |
| --- | --- | --- |
| Home | `views:HomePage` | `home` |
| Preferences | `views:NotificationPreferencesPage` | `preferences` |
| Device | `views:DeviceInfoPage` | `device` |

`Title="Push Notification Sample"`. The code-behind only calls `InitializeComponent()`.

---

## 5. Data models — complete reference

### 5.1 `DeviceInstallation` (`Models/DeviceInstallation.cs`)

A point-in-time snapshot of the device. All properties are settable.

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `InstallationId` | `string` | `""` | Stable per-install identifier |
| `DeviceToken` | `string` | `""` | FCM/APNs token |
| `Platform` | `PlatformType` | `Unknown` | Which platform produced the token |
| `PlatformName` | `string` | `""` | Raw platform string, e.g. "Android" |
| `Model` | `string` | `""` | Hardware model |
| `Manufacturer` | `string` | `""` | Manufacturer |
| `DeviceName` | `string` | `""` | User-recognisable device name |
| `OsVersion` | `string` | `""` | e.g. "Android 15" |
| `AppVersion` | `string` | `""` | Display version |
| `AppBuild` | `string` | `""` | Build number |
| `BundleId` | `string` | `""` | Package/bundle id |
| `Language` | `string` | `""` | Two-letter ISO language |
| `TimeZone` | `string` | `""` | IANA time zone id |
| `Locale` | `string` | `""` | Locale, e.g. "en-US" |
| `ScreenResolution` | `string` | `""` | `"{width}x{height}"` in DIPs |
| `ScreenDensity` | `double` | `0` | Pixel density |
| `DeviceType` | `string` | `""` | Idiom: Phone/Tablet/Desktop |
| `IsEmulator` | `bool` | `false` | Simulator/emulator flag |
| `CapturedAtUtc` | `DateTimeOffset` | `UtcNow` | Capture timestamp |

### 5.2 `DeviceRegistrationRequest` (`Models/DeviceRegistrationRequest.cs`)

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `InstallationId` | `string` | `""` | Installation identifier |
| `DeviceToken` | `string` | `""` | FCM/APNs token |
| `Platform` | `PlatformType` | `Unknown` | Platform |
| `UserId` | `string?` | `null` | Signed-in user, if any |
| `Topics` | `List<string>` | `new()` | Topics to receive at registration |
| `AppVersion` | `string` | `""` | App version for diagnostics |

### 5.3 `DeviceRegistrationResult` (`Models/DeviceRegistrationResult.cs`)

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Success` | `bool` | `false` | Whether the call succeeded |
| `InstallationId` | `string?` | `null` | Server-side installation id |
| `Message` | `string?` | `null` | Human-readable status or error |
| `ProcessedAtUtc` | `DateTimeOffset` | `UtcNow` | When the result was produced |

Factory methods:

- `static DeviceRegistrationResult Ok(string installationId, string message = "Registered")`
  — returns a successful result.
- `static DeviceRegistrationResult Fail(string message)` — returns a failed result.

### 5.4 `NotificationPermissionStatus` (`Models/NotificationPermissionStatus.cs`)

| Value | Number | Meaning |
| --- | --- | --- |
| `NotRequested` | 0 | Never requested from the user |
| `Granted` | 1 | User granted permission |
| `Denied` | 2 | User denied permission |
| `Suppressed` | 3 | Granted but suppressed at the OS level |
| `Unknown` | 4 | Platform could not determine the state |

### 5.5 `NotificationPreference` (`Models/NotificationPreference.cs`)

Constructor `NotificationPreference(NotificationTopic topic, string displayName, string description)`.

| Member | Type | Access | Meaning |
| --- | --- | --- | --- |
| `Topic` | `NotificationTopic` | get | Topic identity |
| `DisplayName` | `string` | get | Label shown in the UI |
| `Description` | `string` | get | Short explanation |
| `IsEnabled` | `bool` | get/set | Whether the user opted in |

### 5.6 `NotificationTopic` (`Models/NotificationTopic.cs`)

Enum. String names are the canonical topic names:

`Announcements`, `AccountAlerts`, `OrderUpdates`, `Promotions`, `Tips`, `WeeklyDigest`.

### 5.7 `PlatformType` (`Models/PlatformType.cs`)

`Unknown = 0`, `Android = 1`, `iOS = 2`, `Windows = 3`, `MacCatalyst = 4`.

### 5.8 `PushConfiguration` (`Models/PushConfiguration.cs`)

Runtime provider configuration passed to `IPushNotificationService.InitializeAsync`.

| Member | Type | Default |
| --- | --- | --- |
| `FcmSenderId` | `string?` | `null` |
| `FcmNotificationChannelId` | `string` | `"general"` |
| `FcmNotificationChannelName` | `string` | `"General"` |
| `ApnsEnvironment` | `string` | `"development"` |
| `ApnsTopic` | `string` | `""` |
| `ApiBaseUrl` | `string` | `"https://api.example.com"` |

### 5.9 `PushMessage` (`Models/PushMessage.cs`)

| Member | Type | Default | Meaning |
| --- | --- | --- | --- |
| `MessageId` | `string?` | `null` | Provider message id |
| `Title` | `string?` | `null` | Notification title |
| `Body` | `string?` | `null` | Notification body |
| `Topic` | `string?` | `null` | Topic/sender, when reported |
| `Data` | `IDictionary<string,string>` | `new Dictionary<...>()` | Custom payload |
| `WasForeground` | `bool` | `false` | Arrived while app was foreground |
| `ReceivedAtUtc` | `DateTimeOffset` | `UtcNow` | Receive time |

### 5.10 `PushPermissionOutcome` (`Models/PushPermissionOutcome.cs`)

| Value | Number | Meaning |
| --- | --- | --- |
| `Unavailable` | 0 | No visible page to host the soft prompt |
| `SoftPromptDeclined` | 1 | User declined or dismissed the soft prompt; OS not prompted |
| `OsPermissionDenied` | 2 | OS dialog reported a denial |
| `OsPermissionGranted` | 3 | User allowed; preferences may be shown |

A soft-prompt decline is deliberately distinct from an OS denial so the OS dialog can still
be shown later.

---

## 6. Configuration

### 6.1 `PushOptions` (`Configuration/PushOptions.cs`)

| Member | Type | Default |
| --- | --- | --- |
| `SectionName` (const) | `string` | `"Push"` |
| `ApiBaseUrl` | `string` | `"https://api.example.com"` |
| `FcmSenderId` | `string?` | `null` |
| `FcmNotificationChannelId` | `string` | `"general"` |
| `FcmNotificationChannelName` | `string` | `"General"` |
| `ApnsEnvironment` | `string` | `"development"` |
| `ApnsTopic` | `string` | `"com.companyname.pushnotification.app"` |

### 6.2 `appsettings.json`

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

Every value can be overridden with a `PUSH_`-prefixed environment variable. Nested keys use
a double underscore, for example `PUSH_Push__FcmSenderId`.

---

## 7. Service reference — complete reference

### 7.1 `IDeviceInfoProvider` / `DeviceInfoProvider`

`Services/IDeviceInfoProvider.cs`, `Services/DeviceInfoProvider.cs`,
`Platforms/*/DeviceInfoProvider.Platform.cs`.

`DeviceInfoProvider` is a `sealed partial class`. The shared part supplies everything except
the platform slice, which each target implements in `GetPlatformDetails`.

#### Constructor `DeviceInfoProvider(IPreferences preferences)`

Stores the preferences store used for the installation id.

#### `public string GetOrCreateInstallationId()`

1. Reads `pn.installation_id` from `IPreferences` with a `null` default.
2. If the value is non-blank, returns it.
3. Otherwise generates `Guid.NewGuid().ToString("D")`, persists it under
   `pn.installation_id`, and returns it.

The value survives restarts and token refreshes; it changes on uninstall/reinstall.

#### `public DeviceInstallation GetDeviceInfo()`

Builds the snapshot:

1. `GetPlatform()` — maps the MAUI `DevicePlatform` to `PlatformType`.
2. `GetPlatformDetails()` — the platform partial supplies model, manufacturer and emulator.
3. `Microsoft.Maui.Devices.DeviceInfo.Current`, `AppInfo.Current`, `DeviceDisplay.MainDisplayInfo`
   and `CultureInfo` supply the rest.
4. Returns a `DeviceInstallation` with all fields populated and `CapturedAtUtc = UtcNow`.

#### `private static PlatformType GetPlatform()`

Comparison chain over `DeviceInfo.Current.Platform`: `Android` -> `Android`, `iOS` -> `iOS`,
`WinUI` -> `Windows`, `MacCatalyst` -> `MacCatalyst`, otherwise `Unknown`. A comparison chain
is used rather than a `switch` because `DevicePlatform` is a struct, not an enum.

#### `private static string GetScreenResolution()`

Returns `"{display.Width}x{display.Height}"` from `DeviceDisplay.MainDisplayInfo`.

#### `private partial PlatformDeviceDetails GetPlatformDetails()`

Declared in the shared file, implemented per platform.

#### `public readonly record struct PlatformDeviceDetails(string Model, string Manufacturer, bool IsEmulator)`

Container for the platform slice.

### 7.2 `IDeviceRegistrationService` / `DummyDeviceRegistrationService`

`Services/Api/IDeviceRegistrationService.cs`, `Services/Api/DummyDeviceRegistrationService.cs`.

Interface methods:

- `Task<DeviceRegistrationResult> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default)`
- `Task<DeviceRegistrationResult> SaveInstallationAsync(DeviceInstallation installation, CancellationToken cancellationToken = default)`
- `Task<DeviceRegistrationResult> UnregisterDeviceAsync(string installationId, CancellationToken cancellationToken = default)`
- `Task<DeviceRegistrationResult> SaveNotificationPreferencesAsync(string installationId, IReadOnlyCollection<NotificationTopic> enabledTopics, CancellationToken cancellationToken = default)`

Dummy implementation. Static `SerializerOptions` use `WriteIndented = true` and a
`JsonStringEnumConverter`.

#### Constructor `DummyDeviceRegistrationService(ILogger<DummyDeviceRegistrationService> logger)`

Stores the logger.

#### `public async Task<DeviceRegistrationResult> RegisterDeviceAsync(...)`

1. `ArgumentNullException.ThrowIfNull(request)`.
2. If `request.DeviceToken` is blank, logs a warning and returns
   `Fail("A device token is required to register.")`.
3. `await SimulateNetworkAsync(cancellationToken)`.
4. Logs `"[DUMMY] POST /api/devices/register"` plus the serialized request.
5. Returns `Ok(request.InstallationId, "Device token registered.")`.

#### `public async Task<DeviceRegistrationResult> SaveInstallationAsync(...)`

1. `ArgumentNullException.ThrowIfNull(installation)`.
2. `await SimulateNetworkAsync(cancellationToken)`.
3. Logs `"[DUMMY] PUT /api/installations/{id}"` plus the serialized installation.
4. Returns `Ok(installation.InstallationId, "Installation saved.")`.

#### `public async Task<DeviceRegistrationResult> UnregisterDeviceAsync(...)`

1. `ArgumentException.ThrowIfNullOrWhiteSpace(installationId)`.
2. `await SimulateNetworkAsync(cancellationToken)`.
3. Logs `"[DUMMY] DELETE /api/installations/{id}"`.
4. Returns `Ok(installationId, "Installation removed.")`.

#### `public async Task<DeviceRegistrationResult> SaveNotificationPreferencesAsync(...)`

1. `ArgumentException.ThrowIfNullOrWhiteSpace(installationId)`.
2. `ArgumentNullException.ThrowIfNull(enabledTopics)`.
3. `await SimulateNetworkAsync(cancellationToken)`.
4. Logs `"[DUMMY] PUT /api/installations/{id}/preferences"` with `{ topics: [...] }`.
5. Returns `Ok(installationId, "Preferences saved.")`.

#### `private static Task SimulateNetworkAsync(CancellationToken cancellationToken)`

`Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken)` to simulate latency.

### 7.3 `INotificationPreferencesService` / `NotificationPreferencesService`

`Services/Preferences/INotificationPreferencesService.cs`,
`Services/Preferences/NotificationPreferencesService.cs`.

Static catalog (topic, display name, description):

| Topic | Display | Description |
| --- | --- | --- |
| `Announcements` | Product announcements | News about releases and major changes. |
| `AccountAlerts` | Account alerts | Security, sign-in and billing alerts. |
| `OrderUpdates` | Order updates | Shipping and delivery status changes. |
| `Promotions` | Promotions | Offers, discounts and limited-time deals. |
| `Tips` | Tips and guides | How-to content and feature highlights. |
| `WeeklyDigest` | Weekly digest | A single summary email-style push each week. |

#### Constructor `NotificationPreferencesService(IPreferences, IPushNotificationService, IDeviceRegistrationService, IDeviceInfoProvider, ILogger<NotificationPreferencesService>)`

Stores the five dependencies.

#### `public Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(CancellationToken = default)`

1. `cancellationToken.ThrowIfCancellationRequested()`.
2. Projects the catalog into `NotificationPreference` objects, each with
   `IsEnabled = IsEnabled(topic)`.
3. Returns the list.

#### `public bool IsEnabled(NotificationTopic topic)`

Reads `pn.topic.{topic}` from preferences with `DefaultEnabled(topic)` as the default.

#### `public async Task<IReadOnlyList<NotificationPreference>> SetEnabledAsync(NotificationTopic topic, bool enabled, CancellationToken = default)`

1. Writes `pn.topic.{topic}`.
2. `await ApplySubscriptionAsync(topic, enabled, cancellationToken)`.
3. `await PersistToBackendAsync(cancellationToken)`.
4. Returns `await GetPreferencesAsync(cancellationToken)`.

#### `public async Task<IReadOnlyList<NotificationPreference>> SetAllAsync(bool enabled, CancellationToken = default)`

1. For every catalog entry: writes `pn.topic.{topic}` then
   `await ApplySubscriptionAsync(entry.Topic, enabled, cancellationToken)`.
2. `await PersistToBackendAsync(cancellationToken)`.
3. Returns `await GetPreferencesAsync(cancellationToken)`.

#### `public IReadOnlyCollection<NotificationTopic> GetEnabledTopics()`

Returns the topics whose `IsEnabled` is `true`.

#### `public async Task SynchronizeSubscriptionsAsync(CancellationToken = default)`

For every catalog entry, `await ApplySubscriptionAsync(entry.Topic, IsEnabled(entry.Topic), cancellationToken)`.

#### `private async Task ApplySubscriptionAsync(NotificationTopic topic, bool enabled, CancellationToken)`

Calls `SubscribeToTopicAsync` or `UnsubscribeFromTopicAsync` on the push service. A thrown
exception is caught and logged as a warning so a failed subscription never blocks saving the
local choice.

#### `private async Task PersistToBackendAsync(CancellationToken)`

1. `installationId = _deviceInfoProvider.GetOrCreateInstallationId()`.
2. `await _registrationService.SaveNotificationPreferencesAsync(installationId, GetEnabledTopics(), cancellationToken)`.
3. A thrown exception is caught and logged as a warning.

#### `private static string PreferenceKey(NotificationTopic topic)`

Returns `$"pn.topic.{topic}"`.

#### `private static bool DefaultEnabled(NotificationTopic topic)`

`true` for `AccountAlerts` and `OrderUpdates`; `false` otherwise.

### 7.4 `IPushNotificationService`

`Services/Notifications/IPushNotificationService.cs`. Contract implemented per platform.

| Member | Signature |
| --- | --- |
| `TokenRefreshed` | `event EventHandler<string>?` |
| `MessageReceived` | `event EventHandler<PushMessage>?` |
| `NotificationTapped` | `event EventHandler<PushMessage>?` |
| `InitializeAsync` | `Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken cancellationToken = default)` |
| `GetDeviceTokenAsync` | `Task<string?> GetDeviceTokenAsync(CancellationToken cancellationToken = default)` |
| `SubscribeToTopicAsync` | `Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)` |
| `UnsubscribeFromTopicAsync` | `Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken cancellationToken = default)` |
| `DeleteTokenAsync` | `Task DeleteTokenAsync(CancellationToken cancellationToken = default)` |

### 7.5 `INotificationPermissionService`

`Services/Notifications/INotificationPermissionService.cs`.

- `Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken cancellationToken = default)`
  — reads the current permission without prompting.
- `Task<NotificationPermissionStatus> RequestAsync(CancellationToken cancellationToken = default)`
  — prompts the user when the platform allows it and returns the resulting status.

### 7.6 `UnsupportedPushNotificationService`

`Services/Notifications/UnsupportedPushNotificationService.cs`. Desktop fallback.

- Declares `TokenRefreshed`, `MessageReceived` and `NotificationTapped`; the compiler warning
  about unused events is suppressed with `#pragma warning disable CS0067`.
- `InitializeAsync` returns `false`.
- `GetDeviceTokenAsync` returns `null`.
- `SubscribeToTopicAsync`, `UnsubscribeFromTopicAsync` and `DeleteTokenAsync` return
  `Task.CompletedTask`.

### 7.7 `AlwaysGrantedNotificationPermissionService`

`Services/Notifications/AlwaysGrantedNotificationPermissionService.cs`. Desktop fallback.
Both `GetStatusAsync` and `RequestAsync` return `NotificationPermissionStatus.Granted`.

### 7.8 `IPushRegistrationCoordinator` / `PushRegistrationCoordinator`

`Services/PushRegistrationCoordinator.cs`.

#### Interface

| Member | Signature |
| --- | --- |
| `CurrentDeviceToken` | `string? { get; }` |
| `CurrentInstallation` | `DeviceInstallation? { get; }` |
| `RegisterAsync` | `Task<DeviceRegistrationResult> RegisterAsync(bool requestPermission = true, CancellationToken cancellationToken = default)` |
| `SaveInstallationAsync` | `Task<DeviceRegistrationResult> SaveInstallationAsync(CancellationToken cancellationToken = default)` |
| `UnregisterAsync` | `Task UnregisterAsync(CancellationToken cancellationToken = default)` |

#### Constructor `PushRegistrationCoordinator(IPushNotificationService, INotificationPermissionService, IDeviceRegistrationService, INotificationPreferencesService, IDeviceInfoProvider, IOptions<PushOptions>, ILogger<PushRegistrationCoordinator>)`

Stores the dependencies and subscribes to `_pushNotificationService.TokenRefreshed` with
`OnTokenRefreshed`.

#### `public string? CurrentDeviceToken { get; private set; }`

The most recent token, or `null`.

#### `public DeviceInstallation? CurrentInstallation { get; private set; }`

The most recent snapshot.

#### `public async Task<DeviceRegistrationResult> RegisterAsync(bool requestPermission = true, CancellationToken = default)`

1. `permission = requestPermission ? await RequestAsync(...) : await GetStatusAsync(...)`.
2. If `permission != Granted`, logs and returns `Fail($"Notification permission is {permission}.")`.
3. `await _pushNotificationService.InitializeAsync(BuildPushConfiguration(), cancellationToken)`.
4. `token = await _pushNotificationService.GetDeviceTokenAsync(cancellationToken)`.
5. If the token is blank, logs and returns `Fail("Device token is not available yet.")`.
6. Sets `CurrentDeviceToken` and returns `await RegisterWithBackendAsync(token, cancellationToken)`.

#### `public async Task<DeviceRegistrationResult> SaveInstallationAsync(CancellationToken = default)`

1. `installation = _deviceInfoProvider.GetDeviceInfo()`.
2. Sets `installation.DeviceToken = CurrentDeviceToken ?? ""`.
3. Stores it in `CurrentInstallation`.
4. Returns `await _registrationService.SaveInstallationAsync(installation, cancellationToken)`.

#### `public async Task UnregisterAsync(CancellationToken = default)`

1. `installationId = _deviceInfoProvider.GetOrCreateInstallationId()`.
2. `await _registrationService.UnregisterDeviceAsync(installationId, cancellationToken)`.
3. `await _pushNotificationService.DeleteTokenAsync(cancellationToken)`.
4. Sets `CurrentDeviceToken = null`.

#### `private async Task<DeviceRegistrationResult> RegisterWithBackendAsync(string token, CancellationToken)`

1. Builds the installation snapshot, sets `DeviceToken = token`, stores it in
   `CurrentInstallation`.
2. Builds a `DeviceRegistrationRequest` with `InstallationId`, `DeviceToken`, `Platform`,
   `AppVersion` and `Topics = GetEnabledTopics()` (as strings).
3. `registrationResult = await _registrationService.RegisterDeviceAsync(request, cancellationToken)`.
4. `await _registrationService.SaveInstallationAsync(installation, cancellationToken)`.
5. `await _preferencesService.SynchronizeSubscriptionsAsync(cancellationToken)`.
6. Returns `registrationResult`.

#### `private PushConfiguration BuildPushConfiguration()`

Copies the `PushOptions` values into a `PushConfiguration`.

#### `private async void OnTokenRefreshed(object? sender, string token)`

1. Stores the new token in `CurrentDeviceToken`.
2. Logs "Push token refreshed.".
3. `await RegisterWithBackendAsync(token, CancellationToken.None)`.
4. A thrown exception is caught and logged as a warning.

This is an `async void` event handler; all exceptions are caught inside the method.

### 7.9 `IPushPermissionFlowService` / `PushPermissionFlowService`

`Services/PushPermissionFlowService.cs`.

#### `public sealed record PushPermissionFlowResult(PushPermissionOutcome Outcome, NotificationPermissionStatus PermissionStatus, DeviceRegistrationResult? RegistrationResult)`

- `public bool IsGranted => Outcome == PushPermissionOutcome.OsPermissionGranted;`

#### Interface

- `Task<PushPermissionFlowResult> RequestPermissionWithSoftPromptAsync(bool showPreferencesOnGrant = true, CancellationToken cancellationToken = default)`

#### Constructor `PushPermissionFlowService(INotificationPermissionService, IPushRegistrationCoordinator, INotificationPreferencesDialogService, ILogger<PushPermissionFlowService>)`

Stores the four dependencies.

#### `public async Task<PushPermissionFlowResult> RequestPermissionWithSoftPromptAsync(bool showPreferencesOnGrant = true, CancellationToken = default)`

1. `page = Application.Current?.Windows.FirstOrDefault()?.Page`. If `null`, logs a warning
   and returns `new PushPermissionFlowResult(Unavailable, Unknown, null)`.
2. `agrees = await ShowSoftPromptAsync(page)`. If `false`, logs and returns
   `new PushPermissionFlowResult(SoftPromptDeclined, NotRequested, null)`.
3. `status = await _permissionService.RequestAsync(cancellationToken)`. If not `Granted`,
   logs and returns `new PushPermissionFlowResult(OsPermissionDenied, status, null)`.
4. `registration = await _coordinator.RegisterAsync(requestPermission: false, cancellationToken)`.
5. When `showPreferencesOnGrant`, `await _preferencesDialog.ShowAsync()`.
6. Returns `new PushPermissionFlowResult(OsPermissionGranted, status, registration)`.

#### `private static async Task<bool> ShowSoftPromptAsync(Page page)`

1. `prompt = new NotificationSoftPromptPage()` — the customisable popup page.
2. `await MainThread.InvokeOnMainThreadAsync(() => page.Navigation.PushModalAsync(prompt))`.
3. `agrees = await prompt.Result` — waits for Yes / No.
4. `await MainThread.InvokeOnMainThreadAsync(() => page.Navigation.PopModalAsync())`.
5. Returns `agrees`.

Returns `true` only for Yes. A No tap, a scrim tap or a back press all resolve to `false`.

### 7.10 `INotificationPreferencesDialogService` / `NotificationPreferencesDialogService`

`Views/NotificationPreferencesDialogService.cs`.

- `Task ShowAsync(CancellationToken cancellationToken = default)` — resolves the visible
  page; if `null` returns; otherwise creates `NotificationPreferencesPopupPage` with the
  shared `NotificationPreferencesViewModel` and calls `page.Navigation.PushModalAsync(popup)`.

The constructor takes `NotificationPreferencesViewModel`.

---

## 8. Platform reference — complete reference

### 8.1 Android

#### `MainActivity` (`Platforms/Android/MainActivity.cs`)

Declared with `[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ScreenSize | Orientation | UiMode | ScreenLayout | SmallestScreenSize | Density)]`.

- `protected override void OnCreate(Bundle? savedInstanceState)` — calls `base.OnCreate`,
  then `HandleNotificationIntent(Intent)`.
- `protected override void OnNewIntent(Intent? intent)` — calls `base.OnNewIntent`, then
  `HandleNotificationIntent(intent)`.
- `public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)`
  — forwards to `AndroidNotificationPermissionService.HandlePermissionResult(permissions, grantResults)`
  then calls `base`.
- `private static void HandleNotificationIntent(Intent? intent)` — if the intent has extras
  containing `google.message_id`, calls
  `FirebasePushBridge.RaiseNotificationTapped(FirebaseMessageMapper.MapIntent(intent))`.

#### `MainApplication` (`Platforms/Android/MainApplication.cs`)

`[Application] public class MainApplication : MauiApplication`. The constructor forwards the
handle/ownership to the base. `CreateMauiApp()` returns `MauiProgram.CreateMauiApp()`.

#### `AndroidNotificationPermissionService` (`Platforms/Android/AndroidNotificationPermissionService.cs`)

`private const int RequestCode = 0x5100`. `private static TaskCompletionSource<NotificationPermissionStatus>? _pendingRequest`.

- `public Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken = default)`
  — below API 33 returns `Granted`; otherwise checks `POST_NOTIFICATIONS` with
  `ContextCompat.CheckSelfPermission` and returns `Granted` or `Denied`.
- `public Task<NotificationPermissionStatus> RequestAsync(CancellationToken = default)`
  — below API 33 returns `Granted`; if `Platform.CurrentActivity` is `null` returns
  `Unknown`; otherwise creates a `TaskCompletionSource`, calls
  `ActivityCompat.RequestPermissions` with `POST_NOTIFICATIONS` and `RequestCode`, and
  returns the task.
- `internal static void HandlePermissionResult(string[] permissions, Permission[] grantResults)`
  — atomically takes the pending request; if `grantResults[0] == Granted` completes with
  `Granted`, else `Denied`.

#### `AndroidPushNotificationService` (`Platforms/Android/AndroidPushNotificationService.cs`)

`private bool _initialized`.

- `public Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken = default)`
  — on first call subscribes to `FirebasePushBridge.TokenRefreshed`, `.MessageReceived` and
  `.NotificationTapped`, sets `_initialized`, then `CreateNotificationChannel(configuration)`,
  logs, returns `true`.
- `public async Task<string?> GetDeviceTokenAsync(CancellationToken = default)`
  — `await FirebaseMessaging.Instance.GetToken().AsAsync<Java.Lang.String>()`, returns its
  string form. `CS0618` is suppressed around the obsolete token API. Returns `null` on
  exception.
- `public async Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken = default)`
  — `await FirebaseMessaging.Instance.SubscribeToTopic(TopicName(topic)).AsAsync<Java.Lang.Object>()`.
- `public async Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken = default)`
  — the unsubscribe equivalent.
- `public async Task DeleteTokenAsync(CancellationToken = default)`
  — `await FirebaseMessaging.Instance.DeleteToken().AsAsync<Java.Lang.Object>()`.
- `public static string TopicName(NotificationTopic topic)` — `topic.ToString().ToLowerInvariant()`.
- `private void CreateNotificationChannel(PushConfiguration configuration)` — returns below
  API 26; resolves the `NotificationManager`; returns if it is `null` or the channel exists;
  otherwise creates a `NotificationChannel(id, name, NotificationImportance.Default)` with a
  description and calls `CreateNotificationChannel`.
- `private void OnTokenRefreshed(object? sender, string token)` — re-raises `TokenRefreshed`.
- `private void OnMessageReceived(object? sender, PushMessage message)` — re-raises `MessageReceived`.
- `private void OnNotificationTapped(object? sender, PushMessage message)` — re-raises `NotificationTapped`.

#### `AppFirebaseMessagingService` (`Platforms/Android/AppFirebaseMessagingService.cs`)

`[Service(Exported = false)]`, `[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]`.

- `public override void OnNewToken(string token)` — calls base then
  `FirebasePushBridge.RaiseTokenRefreshed(token)`. `CS0618, CS0672` suppressed.
- `public override void OnMessageReceived(RemoteMessage message)` — calls base then
  `FirebasePushBridge.RaiseMessageReceived(FirebaseMessageMapper.Map(message, IsAppInForeground()))`.
- `private static bool IsAppInForeground()` — resolves the `ActivityManager`, finds the
  process whose `ProcessName` equals the package name, and returns whether its `Importance`
  is `Foreground`.

#### `FirebasePushBridge` (`Platforms/Android/FirebasePushBridge.cs`)

`internal static class`.

- Events: `TokenRefreshed`, `MessageReceived`, `NotificationTapped`.
- `public static void RaiseTokenRefreshed(string token)` — `TokenRefreshed?.Invoke(null, token)`.
- `public static void RaiseMessageReceived(PushMessage message)` — `MessageReceived?.Invoke(null, message)`.
- `public static void RaiseNotificationTapped(PushMessage message)` — `NotificationTapped?.Invoke(null, message)`.

#### `FirebaseMessageMapper` (`Platforms/Android/FirebaseMessageMapper.cs`)

`internal static class`.

- `public static PushMessage Map(RemoteMessage message, bool wasForeground)` — copies
  `message.Data` into a dictionary; builds a `PushMessage` with `MessageId`, `Title` (from
  the notification or `data["title"]`), `Body` (notification or `data["body"]`), `Topic`
  (`message.From`), `Data` and `WasForeground`.
- `public static PushMessage MapIntent(Intent intent)` — copies the intent extras into a
  dictionary; builds a `PushMessage` with `MessageId` from `google.message_id`, `Title` from
  `gcm.n.title` or `title`, `Body` from `gcm.n.body` or `body`, `Topic` from `from`, and
  `WasForeground = false`.
- `private static string? Get(IReadOnlyDictionary<string,string> data, string key)` —
  `data.TryGetValue(key, out var value) ? value : null`.

#### `DeviceInfoProvider.Platform` (`Platforms/Android/DeviceInfoProvider.Platform.cs`)

- `private partial PlatformDeviceDetails GetPlatformDetails()` — computes `isEmulator` from
  `Build.Fingerprint` containing "generic", "emulator" or "sdk"; returns model from
  `Build.Model`, manufacturer from `Build.Manufacturer`.

#### `AndroidManifest.xml`

Permissions: `ACCESS_NETWORK_STATE`, `INTERNET`, `POST_NOTIFICATIONS`, `WAKE_LOCK`,
`com.google.android.c2dm.permission.RECEIVE`.

### 8.2 iOS

#### `AppDelegate` (`Platforms/iOS/AppDelegate.cs`)

`[Register("AppDelegate")] public class AppDelegate : MauiUIApplicationDelegate`.

- `protected override MauiApp CreateMauiApp()` — returns `MauiProgram.CreateMauiApp()`.
- `public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)`
  — sets `UNUserNotificationCenter.Current.Delegate = new AppUserNotificationCenterDelegate()`
  then calls base.
- `[Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")] public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)`
  — trims `<`, `>` and spaces from `deviceToken.Description`; when non-blank calls
  `ApplePushBridge.SetDeviceToken(token)`.
- `[Export("application:didFailToRegisterForRemoteNotificationsWithError:")] public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)`
  — writes the localized description to the debug output.
- `[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")] public void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)`
  — raises `ApplePushBridge.RaiseMessageReceived` with a `PushMessage` built from `userInfo`
  and the app state, then completes with `UIBackgroundFetchResult.NewData`.

#### `AppUserNotificationCenterDelegate` (same file)

- `public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)`
  — raises `ApplePushBridge.RaiseMessageReceived` with `WasForeground = true`, then completes
  with `Banner | Sound`.
- `public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)`
  — raises `ApplePushBridge.RaiseNotificationTapped` with `WasForeground = false`, then
  completes.

#### `NSDictionaryExtensions` (same file)

- `public static Dictionary<string,string> ToDictionary(this NSDictionary? dictionary)` —
  returns an empty dictionary for `null`; otherwise copies every non-null value as a string.

#### `AppleNotificationPermissionService` (`Platforms/iOS/AppleNotificationPermissionService.cs`)

- `public async Task<NotificationPermissionStatus> GetStatusAsync(CancellationToken = default)`
  — `await UNUserNotificationCenter.Current.GetNotificationSettingsAsync()` and maps the
  authorization status.
- `public async Task<NotificationPermissionStatus> RequestAsync(CancellationToken = default)`
  — requests `Alert | Badge | Sound`; returns `Denied` when not approved; otherwise returns
  `await GetStatusAsync(cancellationToken)`.
- `private static NotificationPermissionStatus Map(UNAuthorizationStatus status)` —
  `Authorized`/`Provisional`/`Ephemeral` -> `Granted`; `Denied` -> `Denied`;
  `NotDetermined` -> `NotRequested`; otherwise `Unknown`.

#### `ApplePushNotificationService` (`Platforms/iOS/ApplePushNotificationService.cs`)

`private bool _initialized`.

- `public Task<bool> InitializeAsync(PushConfiguration configuration, CancellationToken = default)`
  — on first call subscribes to the three `ApplePushBridge` events, sets `_initialized`;
  then on the main thread calls `UIApplication.SharedApplication.RegisterForRemoteNotifications()`;
  logs; returns `true`.
- `public Task<string?> GetDeviceTokenAsync(CancellationToken = default)` — returns
  `ApplePushBridge.DeviceToken` when non-blank; otherwise asks again on the main thread with
  `RegisterForRemoteNotifications()` and returns the current bridge token.
- `public Task SubscribeToTopicAsync(NotificationTopic topic, CancellationToken = default)`
  — logs "APNs topic enabled" and returns `Task.CompletedTask` (APNs has no server-side topic
  API; the backend enforces topics).
- `public Task UnsubscribeFromTopicAsync(NotificationTopic topic, CancellationToken = default)`
  — logs and returns `Task.CompletedTask`.
- `public Task DeleteTokenAsync(CancellationToken = default)` — on the main thread calls
  `UIApplication.SharedApplication.UnregisterForRemoteNotifications()`.
- `private void OnTokenRefreshed(object? sender, string token)` — re-raises `TokenRefreshed`.
- `private void OnMessageReceived(object? sender, PushMessage message)` — re-raises `MessageReceived`.
- `private void OnNotificationTapped(object? sender, PushMessage message)` — re-raises `NotificationTapped`.

#### `ApplePushBridge` (`Platforms/iOS/ApplePushBridge.cs`)

`internal static class`. `private static readonly object Sync`; `private static string? _deviceToken`.

- Events: `TokenRefreshed`, `MessageReceived`, `NotificationTapped`.
- `public static string? DeviceToken { get; }` — returns `_deviceToken` under `lock (Sync)`.
- `public static void SetDeviceToken(string token)` — stores under `lock (Sync)` then raises
  `TokenRefreshed`.
- `public static void RaiseMessageReceived(PushMessage message)` — raises `MessageReceived`.
- `public static void RaiseNotificationTapped(PushMessage message)` — raises `NotificationTapped`.

#### `DeviceInfoProvider.Platform` (`Platforms/iOS/DeviceInfoProvider.Platform.cs`)

- `private partial PlatformDeviceDetails GetPlatformDetails()` — model from
  `sysctlbyname("hw.machine")` falling back to `UIDevice.CurrentDevice.Model`; emulator when
  the model starts with `x86_64`, `i386` or `arm64`; manufacturer "Apple".
- `private static string? Sysctl(string name)` — allocates 128 bytes, calls
  `sysctlbyname`, and returns the ANSI string or `null`; frees the buffer in `finally`.
- `[DllImport("libc")] private static extern int sysctlbyname(string name, IntPtr oldp, ref nuint oldlenp, IntPtr newp, nuint newlen)`
  — the native binding.

#### `SceneDelegate` / `Program`

- `[Register("SceneDelegate")] public class SceneDelegate : MauiUISceneDelegate` — empty.
- `Program.Main(string[] args)` — `UIApplication.Main(args, null, typeof(AppDelegate))`.

#### `Info.plist`

`LSRequiresIPhoneOS`, `UIDeviceFamily` 1 and 2, `UIRequiredDeviceCapabilities` arm64,
supported orientations, the MAUI scene manifest, `XSAppIconAssets`, and
`UIBackgroundModes` containing `remote-notification`.

#### `Resources/PrivacyInfo.xcprivacy`

Apple privacy manifest listing the accessed API categories `FileTimestamp`, `SystemBootTime`
and `DiskSpace`, each with a reason code.

### 8.3 Mac Catalyst

- `AppDelegate` — `[Register("AppDelegate")] public class AppDelegate : MauiUIApplicationDelegate`;
  `CreateMauiApp()` returns `MauiProgram.CreateMauiApp()`.
- `Program.Main(string[] args)` — `UIApplication.Main(args, null, typeof(AppDelegate))`.
- `SceneDelegate` — empty.
- `DeviceInfoProvider.Platform.GetPlatformDetails()` — model from `DeviceInfo.Current.Model`,
  manufacturer "Apple", `IsEmulator = false`.
- `Info.plist` — device family 2, category lifestyle, scene manifest, icon assets.
- `Entitlements.plist` — App Sandbox enabled plus `com.apple.security.network.client`.

### 8.4 Windows

- `App.xaml.cs` — `namespace PushNotification.App.WinUI`, `partial class App : MauiWinUIApplication`;
  the constructor calls `InitializeComponent()`; `CreateMauiApp()` returns
  `MauiProgram.CreateMauiApp()`.
- `DeviceInfoProvider.Platform.GetPlatformDetails()` — model and manufacturer from
  `DeviceInfo.Current`, `IsEmulator = false`.

Note: both the Windows and Mac Catalyst partials define `GetPlatformDetails()` for
`DeviceInfoProvider`; each is compiled only for its own target.

---

## 9. View models — complete reference

### 9.1 `BaseViewModel` (`ViewModels/BaseViewModel.cs`)

`public abstract class BaseViewModel : INotifyPropertyChanged`.

| Member | Signature | Notes |
| --- | --- | --- |
| `PropertyChanged` | `event PropertyChangedEventHandler?` | From `INotifyPropertyChanged` |
| `IsBusy` | `bool { get; set; }` | Backed by `SetProperty`; used to guard re-entrant commands |
| `StatusMessage` | `string { get; set; }` | Backed by `SetProperty`; default `""` |
| `SetProperty<T>` | `protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)` | Returns `false` when unchanged; otherwise assigns, raises `PropertyChanged`, returns `true` |
| `OnPropertyChanged` | `protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)` | Raises `PropertyChanged` |

The boolean return from `SetProperty` lets a setter run a side effect only on a real change.

### 9.2 `NotificationPreferenceItem` (`ViewModels/NotificationPreferenceItem.cs`)

`public sealed class NotificationPreferenceItem : BaseViewModel`.

- Constructor `NotificationPreferenceItem(NotificationPreference preference, Action<NotificationPreferenceItem> onToggled)`
  — copies `Topic`, `DisplayName`, `Description`, initial `IsEnabled`, and stores the callback.
- `NotificationTopic Topic { get; }`
- `string DisplayName { get; }`
- `string Description { get; }`
- `bool IsEnabled { get; set; }` — the setter calls `SetProperty`; when it returns `true` it
  invokes `_onToggled(this)`.
- `public void SetEnabledSilently(bool enabled)` — updates the backing field and raises
  `PropertyChanged` without invoking the callback. Used by the view model to avoid a feedback
  loop.

### 9.3 `NotificationPreferencesViewModel` (`ViewModels/NotificationPreferencesViewModel.cs`)

`public sealed class NotificationPreferencesViewModel : BaseViewModel`.

- Constructor `NotificationPreferencesViewModel(INotificationPreferencesService, IDeviceRegistrationService, ILogger<NotificationPreferencesViewModel>)`
  — stores dependencies and creates `ToggleAllCommand = new Command<bool>(async enabled => await ToggleAllAsync(enabled))`.
- `ObservableCollection<NotificationPreferenceItem> Preferences { get; }`
- `ICommand ToggleAllCommand { get; }`
- `bool AllEnabled { get; set; }` — the setter calls `SetProperty`; when it returns `true` it
  fires `_ = ToggleAllAsync(value)`.
- `public async Task LoadAsync(CancellationToken cancellationToken = default)` — returns early
  when busy; loads stored preferences; clears and repopulates `Preferences` with items bound
  to `OnItemToggled`; sets `_allEnabled` from whether all rows are enabled; raises
  `PropertyChanged` for `AllEnabled`; sets `StatusMessage` to
  `"{enabled} of {total} topics enabled."`.
- `private async Task ToggleAllAsync(bool enabled)` — returns early when busy; calls
  `SetAllAsync(enabled)`; applies each result with `SetEnabledSilently` matched by topic; sets
  the status. On failure logs and sets `"Could not update preferences."`.
- `private async void OnItemToggled(NotificationPreferenceItem item)` — persists the single
  topic via `SetEnabledAsync`; recomputes `AllEnabled` and raises `PropertyChanged`; sets the
  status. On failure logs, reverts with `item.SetEnabledSilently(!item.IsEnabled)` and sets
  `"Could not update the preference."`.

### 9.4 `HomeViewModel` (`ViewModels/HomeViewModel.cs`)

`public sealed class HomeViewModel : BaseViewModel`.

- Constructor `HomeViewModel(IPushRegistrationCoordinator, IPushNotificationService, INotificationPermissionService, IPushPermissionFlowService, ILogger<HomeViewModel>)`
  — stores dependencies; subscribes to `MessageReceived` and `NotificationTapped`; creates
  `RequestPermissionCommand = new Command(async () => await RequestPermissionAsync())` and
  `RefreshCommand = new Command(async () => await RefreshAsync())`.
- `ObservableCollection<PushMessage> ReceivedMessages { get; }`
- `ICommand RequestPermissionCommand { get; }` — starts the guided flow.
- `ICommand RefreshCommand { get; }`
- `string PermissionStatus { get; private set; }` — default `"Unknown"`.
- `string DeviceToken { get; private set; }` — default `"Not available"`.
- `public async Task LoadAsync()` — `await RefreshAsync()`.
- `private async Task RequestPermissionAsync()` — returns early when busy; sets
  `StatusMessage = "Waiting for your choice..."`; awaits
  `_permissionFlow.RequestPermissionWithSoftPromptAsync()`; maps the outcome:

  | Outcome | Status message |
  | --- | --- |
  | `OsPermissionGranted` | registration message or `"Notifications enabled."` |
  | `OsPermissionDenied` | `"Notifications stay off until you allow them in system settings."` |
  | `SoftPromptDeclined` | `"No problem. You can turn notifications on later."` |
  | otherwise | `"Could not request notification permission."` |

  Then `await RefreshAsync()`. Exceptions are logged and surfaced as
  `"Could not request notification permission."`.
- `private async Task RefreshAsync()` — reads the permission status into `PermissionStatus`;
  resolves the token from `CurrentDeviceToken` or `GetDeviceTokenAsync()`; sets
  `DeviceToken` to `"Not available"` when empty. Exceptions are logged as warnings.
- `private void OnMessageReceived(object? sender, PushMessage message)` — on the main thread
  inserts the message at index 0 and sets the status to
  `"Received: {title|body|messageId}"`.
- `private void OnNotificationTapped(object? sender, PushMessage message)` — on the main
  thread inserts the message and sets `"Opened from notification: {title|body}"`.

### 9.5 `DeviceInfoViewModel` (`ViewModels/DeviceInfoViewModel.cs`)

`public sealed class DeviceInfoViewModel : BaseViewModel`.

- Constructor `DeviceInfoViewModel(IPushRegistrationCoordinator, IDeviceInfoProvider, INotificationPermissionService, ILogger<DeviceInfoViewModel>)`
  — stores dependencies and creates `RegisterCommand`, `SaveInstallationCommand` and
  `UnregisterCommand`.
- `ObservableCollection<DeviceInfoRow> Rows { get; }`
- `ICommand RegisterCommand { get; }`, `SaveInstallationCommand { get; }`, `UnregisterCommand { get; }`
- `string PermissionStatus { get; private set; }` — default `"Unknown"`.
- `public async Task LoadAsync()` — builds the snapshot, attaches `CurrentDeviceToken`,
  calls `PopulateRows`, reads the permission status. Exceptions are logged and surfaced as
  `"Could not load device information."`.
- `private async Task RegisterAsync()` — guards on busy; calls `RegisterAsync()` on the
  coordinator; reports `"Registered: {message}"` or `"Registration incomplete: {message}"`;
  reloads. Exceptions -> `"Registration failed. See logs for details."`.
- `private async Task SaveInstallationAsync()` — guards on busy; calls `SaveInstallationAsync()`;
  reports `"Installation saved: {message}"` or `"Save failed: {message}"`; reloads.
  Exceptions -> `"Could not save the installation."`.
- `private async Task UnregisterAsync()` — guards on busy; calls `UnregisterAsync()`; sets
  `"Device unregistered."`; reloads. Exceptions -> `"Could not unregister the device."`.
- `private void PopulateRows(DeviceInstallation installation)` — clears `Rows` and adds
  fifteen rows: Installation Id, Device Token (`(not available yet)` when empty), Platform,
  Model, Manufacturer, Device Name, OS Version, App Version (`version (build)`), Bundle Id,
  Locale, Language, Time Zone, Screen (`{resolution} @ {density:0.##}x`), Device Type,
  Emulator (Yes/No).
- `private void Add(string label, string value)` — appends a `DeviceInfoRow`.
- `public sealed record DeviceInfoRow(string Label, string Value)` — the row type.

---

## 10. Views — complete reference

### 10.1 `HomePage` (`Views/HomePage.xaml`, `.xaml.cs`)

`x:DataType="vm:HomeViewModel"`, `Title="Push notifications"`. Two-row grid:

- **Row 0** — header stack: title label; a bordered card with `PermissionStatus`
  (`StringFormat='Permission: {0}'`) and `DeviceToken` (truncated to two lines); the
  "Enable notifications" button bound to `RequestPermissionCommand`; the "Refresh status"
  button bound to `RefreshCommand`; the "Received messages" label and a caption.
- **Row 1** — `CollectionView` bound to `ReceivedMessages` with an empty view reading
  "No messages received yet." Each item (typed `models:PushMessage`) shows `Title`, `Body`
  and `ReceivedAtUtc` (`{0:g} UTC`).

Code-behind: constructor takes `HomeViewModel`, sets `BindingContext`; `OnAppearing` calls
`LoadAsync()`.

### 10.2 `NotificationSoftPromptPage` (`Views/NotificationSoftPromptPage.xaml`, `.xaml.cs`)

The customisable soft prompt shown before the operating system permission dialog.
`BackgroundColor="Transparent"`, `Shell.NavBarIsVisible="False"`.

- A scrim `BoxView` (`Color="#99000000"`) with a `TapGestureRecognizer` bound to
  `OnDeclineClicked` — tapping outside the card is a No.
- A rounded `Border` (`MaximumWidthRequest="480"`) containing a `VerticalStackLayout` with
  `Padding="20"` and `Spacing="16"`:
  - `TitleLabel` — default text "Notification Request", bold, size 20.
  - `MessageLabel` — default text "Allow Bansoft to send notification for updates", size 14.
  - A two-column grid with `DeclineButton` ("No") and `AcceptButton` ("Yes").

Code-behind members:

| Member | Signature | Notes |
| --- | --- | --- |
| `_completion` | `private readonly TaskCompletionSource<bool>` | `RunContinuationsAsynchronously` |
| Constructor | `NotificationSoftPromptPage(string title = "Notification Request", string message = "Allow Bansoft to send notification for updates", string acceptText = "Yes", string declineText = "No")` | `InitializeComponent()`, then assigns each label/button text |
| `Result` | `Task<bool> { get; }` | `_completion.Task`; `true` for Yes |
| `OnBackButtonPressed` | `protected override bool OnBackButtonPressed()` | Sets the result to `false`, then calls base |
| `OnAcceptClicked` | `private void OnAcceptClicked(object?, EventArgs)` | `_completion.TrySetResult(true)` |
| `OnDeclineClicked` | `private void OnDeclineClicked(object?, EventArgs)` | `_completion.TrySetResult(false)` |

The page does not dismiss itself; the caller pops the modal after awaiting `Result`. The
optional constructor arguments let the same page render any two-button confirmation.

### 10.3 `NotificationPreferencesView` (`Views/NotificationPreferencesView.xaml`, `.xaml.cs`)

`x:DataType="vm:NotificationPreferencesViewModel"`. The single definition of the preference
UI. It is a `ContentView` whose code-behind constructor takes the view model.

- **Row 0** — header grid: title "Notification preferences", subtitle, and a `Close` button
  (`x:Name="CloseButton"`, `IsVisible="False"`, `Clicked="OnCloseClicked"`).
- **Row 1** — `ScrollView` containing a master-switch card (`Switch` bound to `AllEnabled`)
  and a `CollectionView` of `NotificationPreferenceItem` rows, each with `DisplayName`,
  `Description` and a `Switch` bound to `IsEnabled`.
- **Row 2** — footer with an `ActivityIndicator` bound to `IsBusy` and a `StatusMessage`
  label.

Code-behind members:

| Member | Signature | Notes |
| --- | --- | --- |
| Constructor | `NotificationPreferencesView(NotificationPreferencesViewModel viewModel)` | `InitializeComponent()`, sets `BindingContext` and `_viewModel` |
| `ShowCloseButton` | `bool { get; set; }` | Gets/sets `CloseButton.IsVisible`; `true` in the popup |
| `CloseRequested` | `event EventHandler?` | Raised by `OnCloseClicked` |
| `LoadAsync` | `Task LoadAsync()` | `_viewModel.LoadAsync()` |
| `OnCloseClicked` | `private void OnCloseClicked(object?, EventArgs)` | Raises `CloseRequested` |

### 10.4 `NotificationPreferencesPage` (`Views/NotificationPreferencesPage.xaml`, `.xaml.cs`)

The Preferences tab. `Title="Notification preferences"`. Body is a single
`<ContentView x:Name="Host" />`.

Code-behind: constructor takes `NotificationPreferencesViewModel`, creates a
`NotificationPreferencesView(viewModel)` and assigns it to `Host.Content`; `OnAppearing`
calls `_preferencesView.LoadAsync()`.

### 10.5 `NotificationPreferencesPopupPage` (`Views/NotificationPreferencesPopupPage.xaml`, `.xaml.cs`)

The post-permission popup. `BackgroundColor="Transparent"`, `Shell.NavBarIsVisible="False"`.

- A scrim `BoxView` (`Color="#99000000"`) with a `TapGestureRecognizer` bound to
  `OnScrimTapped`.
- A rounded `Border` named `Card` (`MaximumWidthRequest="520"`, `MaximumHeightRequest="640"`)
  with no XAML content.

Code-behind:

| Member | Signature | Notes |
| --- | --- | --- |
| Constructor | `NotificationPreferencesPopupPage(NotificationPreferencesViewModel viewModel)` | Creates the view with `ShowCloseButton = true`, subscribes `CloseRequested`, assigns `Card.Content` |
| `OnAppearing` | `protected override async void OnAppearing()` | `await _preferencesView.LoadAsync()` |
| `OnCloseRequested` | `private async void OnCloseRequested(object?, EventArgs)` | `await Navigation.PopModalAsync()` |
| `OnScrimTapped` | `private async void OnScrimTapped(object?, TappedEventArgs)` | `await Navigation.PopModalAsync()` |

### 10.6 `NotificationPreferencesDialogService` (`Views/NotificationPreferencesDialogService.cs`)

See [7.10](#710-inotificationpreferencesdialogservice--notificationpreferencesdialogservice).

### 10.7 `DeviceInfoPage` (`Views/DeviceInfoPage.xaml`, `.xaml.cs`)

`x:DataType="vm:DeviceInfoViewModel"`, `Title="Device & registration"`. Three-row grid:

- **Row 0** — title, explanation, and `PermissionStatus`
  (`StringFormat='Notification permission: {0}'`).
- **Row 1** — `CollectionView` of `DeviceInfoRow` items in a two-column grid (140 px label
  column) showing `Label` and `Value`.
- **Row 2** — `ActivityIndicator` bound to `IsBusy`, a `StatusMessage` label, and a
  three-button grid bound to `RegisterCommand`, `SaveInstallationCommand` and
  `UnregisterCommand`.

Code-behind: constructor takes `DeviceInfoViewModel` and sets `BindingContext`;
`OnAppearing` calls `LoadAsync()`.

---

## 11. End-to-end processes

### 11.1 Application startup

```
MAUI host
  -> MauiProgram.CreateMauiApp()
       -> load embedded appsettings.json
       -> add PUSH_ environment variables
       -> Configure<PushOptions>("Push")
       -> RegisterPlatformServices (push + permission for the target)
       -> RegisterSharedServices (device info, registration, preferences,
                                  coordinator, flow, dialog, pages)
  -> App(IServiceProvider)
       -> CreateWindow resolves AppShell from DI
  -> Shell renders the Home tab
       -> HomePage.OnAppearing -> HomeViewModel.LoadAsync -> RefreshAsync
            -> read permission status
            -> resolve current token (may be null)
```

No push provider is initialized at startup. Initialization happens on the first
`RegisterAsync`, which keeps the provider work tied to a user action.

### 11.2 Guided permission request and device registration

```
User taps "Enable notifications" (HomePage)
  -> HomeViewModel.RequestPermissionAsync
       -> PushPermissionFlowService.RequestPermissionWithSoftPromptAsync(showPreferencesOnGrant: true)
            1. resolve the visible page; none -> PushPermissionOutcome.Unavailable
            2. soft prompt: PushModalAsync(NotificationSoftPromptPage)
                 default title "Notification Request", message
                 "Allow Bansoft to send notification for updates", Yes / No buttons
                 "No" / scrim tap / back -> PushPermissionOutcome.SoftPromptDeclined (OS never asked)
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
                      ii.  build DeviceRegistrationRequest
                      iii. IDeviceRegistrationService.RegisterDeviceAsync      -> POST /api/devices/register
                      iv.  IDeviceRegistrationService.SaveInstallationAsync    -> PUT  /api/installations/{id}
                      v.   INotificationPreferencesService.SynchronizeSubscriptionsAsync()
            6. INotificationPreferencesDialogService.ShowAsync()
                 -> PushModalAsync(NotificationPreferencesPopupPage with the shared view model)
       -> HomeViewModel maps the outcome to a status message and refreshes the token display
```

Because the flow calls `RegisterAsync(requestPermission: false)`, the coordinator reads the
permission that was just granted rather than prompting a second time. On iOS the first pass
often returns `Fail("Device token is not available yet.")`; the token callback then drives the
retry described in 11.3. The popup is shown regardless, so the user can pick topics
immediately.

### 11.3 Token refresh

```
Android: FCM rotates the token
  -> AppFirebaseMessagingService.OnNewToken
       -> FirebasePushBridge.RaiseTokenRefreshed
  -> AndroidPushNotificationService.OnTokenRefreshed
  -> PushRegistrationCoordinator.OnTokenRefreshed
       -> CurrentDeviceToken = token
       -> RegisterWithBackendAsync(token, CancellationToken.None)

iOS: APNs issues or refreshes the token
  -> AppDelegate.RegisteredForRemoteNotifications
       -> ApplePushBridge.SetDeviceToken
  -> ApplePushNotificationService.OnTokenRefreshed
  -> PushRegistrationCoordinator.OnTokenRefreshed (same as above)
```

Both paths re-register without prompting. Failures are logged as warnings.

### 11.4 Foreground message receipt

```
Android: FCM delivers a message while the app is visible
  -> AppFirebaseMessagingService.OnMessageReceived
       -> FirebaseMessageMapper.Map(message, IsAppInForeground())
       -> FirebasePushBridge.RaiseMessageReceived
  -> AndroidPushNotificationService.OnMessageReceived
  -> HomeViewModel.OnMessageReceived (MainThread)
       -> insert at top of ReceivedMessages, update status

iOS: a notification arrives while the app is active
  -> AppUserNotificationCenterDelegate.WillPresentNotification
       -> ApplePushBridge.RaiseMessageReceived
  -> ApplePushNotificationService.OnMessageReceived
  -> HomeViewModel.OnMessageReceived (MainThread)
```

### 11.5 Notification tap

```
Android: user taps a notification
  -> MainActivity.OnCreate / OnNewIntent
       -> HandleNotificationIntent (extras contain google.message_id)
       -> FirebaseMessageMapper.MapIntent
       -> FirebasePushBridge.RaiseNotificationTapped
  -> AndroidPushNotificationService.OnNotificationTapped
  -> HomeViewModel.OnNotificationTapped (MainThread)

iOS: user taps a notification
  -> AppUserNotificationCenterDelegate.DidReceiveNotificationResponse
       -> ApplePushBridge.RaiseNotificationTapped
  -> ApplePushNotificationService.OnNotificationTapped
  -> HomeViewModel.OnNotificationTapped (MainThread)
```

Both handlers insert the message at the top of the list and set a status noting the app was
opened from a notification.

### 11.6 Changing a notification preference

```
User toggles a row (Preferences tab or popup)
  -> NotificationPreferenceItem.IsEnabled setter -> _onToggled(item)
  -> NotificationPreferencesViewModel.OnItemToggled
       -> INotificationPreferencesService.SetEnabledAsync(topic, enabled)
            1. IPreferences.Set("pn.topic.{topic}", enabled)
            2. ApplySubscriptionAsync -> IPushNotificationService.SubscribeToTopicAsync
                                         or UnsubscribeFromTopicAsync
            3. PersistToBackendAsync -> IDeviceRegistrationService.SaveNotificationPreferencesAsync
       -> recompute AllEnabled, update status
  -> on failure revert the row with SetEnabledSilently(!IsEnabled)

User toggles the master switch
  -> AllEnabled setter -> ToggleAllAsync(enabled)
       -> INotificationPreferencesService.SetAllAsync(enabled)
            (per topic: persist + apply subscription; then one backend sync)
       -> apply each result with SetEnabledSilently
```

### 11.7 Register / save / unregister from the Device tab

```
"Register"  -> DeviceInfoViewModel.RegisterAsync
                 -> IPushRegistrationCoordinator.RegisterAsync(requestPermission: true)
                 -> report result, reload rows
"Save"      -> DeviceInfoViewModel.SaveInstallationAsync
                 -> IPushRegistrationCoordinator.SaveInstallationAsync
                    -> IDeviceInfoProvider.GetDeviceInfo + current token
                    -> IDeviceRegistrationService.SaveInstallationAsync
                 -> report result, reload rows
"Unregister"-> DeviceInfoViewModel.UnregisterAsync
                 -> IPushRegistrationCoordinator.UnregisterAsync
                    -> IDeviceRegistrationService.UnregisterDeviceAsync
                    -> IPushNotificationService.DeleteTokenAsync
                    -> CurrentDeviceToken = null
                 -> report result, reload rows
```

### 11.8 Opening the preference popup

```
INotificationPreferencesDialogService.ShowAsync
  -> resolve Application.Current.Windows.First().Page
  -> new NotificationPreferencesPopupPage(shared NotificationPreferencesViewModel)
  -> page.Navigation.PushModalAsync(popup)
  -> popup.OnAppearing -> NotificationPreferencesView.LoadAsync
  -> user closes (Close button -> CloseRequested -> PopModalAsync,
                  or taps the scrim -> OnScrimTapped -> PopModalAsync)
```

---

## 12. Backend contract

`IDeviceRegistrationService` maps to these HTTP operations:

| Method | HTTP equivalent |
| --- | --- |
| `RegisterDeviceAsync` | `POST /api/devices/register` |
| `SaveInstallationAsync` | `PUT /api/installations/{installationId}` |
| `UnregisterDeviceAsync` | `DELETE /api/installations/{installationId}` |
| `SaveNotificationPreferencesAsync` | `PUT /api/installations/{installationId}/preferences` |

### 12.1 Registration payload

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

`platform` is serialized as a string because the dummy service uses
`JsonStringEnumConverter`.

### 12.2 Installation record

Fields on `DeviceInstallation`: `InstallationId`, `DeviceToken`, `Platform`, `PlatformName`,
`Model`, `Manufacturer`, `DeviceName`, `OsVersion`, `AppVersion`, `AppBuild`, `BundleId`,
`Language`, `TimeZone`, `Locale`, `ScreenResolution`, `ScreenDensity`, `DeviceType`,
`IsEmulator`, `CapturedAtUtc`.

### 12.3 Preferences payload

```json
{ "topics": ["Announcements", "AccountAlerts"] }
```

### 12.4 Topic names

See [Appendix B](#appendix-b--notification-topic-catalog).

---

## 13. Extension points

### 13.1 Real HTTP registration client

Implement `IDeviceRegistrationService` with an `HttpClient` and register it in
`MauiProgram.RegisterSharedServices` instead of `DummyDeviceRegistrationService`. The
interface models the four operations the app needs, so no view model changes are required.

### 13.2 Real topic subscriptions on iOS

`ApplePushNotificationService.SubscribeToTopicAsync` and `UnsubscribeFromTopicAsync` are
no-ops because APNs has no server-side topic API. Call your backend from those methods if the
server needs to track topic membership.

### 13.3 Wiring a real provider on desktop

Replace `UnsupportedPushNotificationService` and `AlwaysGrantedNotificationPermissionService`
in the `#else` branch of `RegisterPlatformServices` with real implementations.

### 13.4 Real authentication

Populate `DeviceRegistrationRequest.UserId` from the signed-in user before registering, and
send an authorization header from the real `IDeviceRegistrationService`.

### 13.5 Customising the soft prompt

`NotificationSoftPromptPage` is a real page, so its wording, styling, buttons and layout can
be changed freely without touching the permission flow. Pass a different `title`, `message`,
`acceptText` or `declineText` to the constructor, or edit the XAML directly. To use a
different page entirely, replace the `new NotificationSoftPromptPage()` line in
`PushPermissionFlowService.ShowSoftPromptAsync` with your own page that exposes a
`Task<bool> Result`.

---

## 14. Build, run and troubleshooting

### 14.1 Build commands

```bash
# Android
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-android

# iOS (macOS only)
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-ios

# Windows
dotnet build src/PushNotification.App/PushNotification.App.csproj -f net10.0-windows10.0.19041.0
```

The verified configuration is Android Debug with 0 warnings and 0 errors.

### 14.2 Android setup

1. Create a Firebase project and add an Android app with package name
   `com.companyname.pushnotification.app` (or change `ApplicationId`).
2. Download `google-services.json` to
   `src/PushNotification.App/Platforms/Android/google-services.json`.
3. Optionally set `Push:FcmSenderId` from the project number.

`google-services.json` is gitignored because it contains an API key.

### 14.3 iOS setup

1. Create an APNs key or certificate and enable Push Notifications for the App ID.
2. Add the Push Notifications capability (an `aps-environment` entitlement).
3. Set `Push:ApnsTopic` to the bundle identifier.
4. Test on a physical device; the simulator cannot receive pushes.

### 14.4 Troubleshooting

| Symptom | Likely cause | Action |
| --- | --- | --- |
| `uses-sdk:minSdkVersion 21 cannot be smaller than version 23` | Android min SDK below library requirement | Keep Android `SupportedOSPlatformVersion` at 24 or higher |
| `Type androidx.fragment.app.FragmentKt is defined multiple times` | AndroidX version mismatch from Firebase | Keep the AndroidX pins aligned with resolved versions |
| `Fail("Device token is not available yet.")` | Token not issued yet, or Firebase config missing | Add `google-services.json`, confirm network access, retry after the token callback |
| Permission always `Denied` on Android | `POST_NOTIFICATIONS` not granted | Confirm the manifest entry and that `MainActivity.OnRequestPermissionsResult` forwards to `HandlePermissionResult` |
| No messages while the app is open on iOS | Presentation options not returned | Confirm `WillPresentNotification` completes with `Banner \| Sound` |
| `Items that are outside Target elements must have one of the following operations` | `RuntimeIdentifier` placed in an `ItemGroup` | Keep it in a `PropertyGroup` |
| Soft prompt never appears | No visible page | The flow returns `Unavailable`; ensure it runs from a visible page |
| Preferences not reflected on the server | Dummy service only logs | Expected; see 13.1 |

---

## 15. Known limitations

1. **Dummy backend.** No network calls are made; the registration and preferences flows log
   payloads only. Replacing the service is a single registration change.
2. **APNs topic subscriptions.** APNs has no server-side topic API, so
   `SubscribeToTopicAsync`/`UnsubscribeFromTopicAsync` only log. Topic targeting must be
   enforced by the backend.
3. **iOS not compiled in this environment.** The build host has only the Android workload, so
   the iOS, Mac Catalyst and Windows targets are compiled only on their native hosts.
4. **Desktop push is a no-op.** Windows and Mac Catalyst run the shared UI but do not receive
   push notifications.
5. **No automated tests.** Validation is `dotnet build`. The dummy services make it
   straightforward to add unit tests around the coordinator, the flow service and the
   preferences service by substituting the interfaces.
6. **Not production hardened.** This is a sample generated by an AI agent (OpenHands) on
   behalf of the repository owner. Review security, error handling and retry behaviour before
   production use.

---

## Appendix A — complete member index

Alphabetical index of the notable members and where they are defined.

| Member | Type | File |
| --- | --- | --- |
| `Add` | private method | `ViewModels/DeviceInfoViewModel.cs` |
| `AllEnabled` | property | `ViewModels/NotificationPreferencesViewModel.cs` |
| `AppDelegate` | class | `Platforms/iOS/AppDelegate.cs`, `Platforms/MacCatalyst/AppDelegate.cs` |
| `AppleNotificationPermissionService` | class | `Platforms/iOS/AppleNotificationPermissionService.cs` |
| `ApplePushBridge` | static class | `Platforms/iOS/ApplePushBridge.cs` |
| `ApplePushNotificationService` | class | `Platforms/iOS/ApplePushNotificationService.cs` |
| `AppUserNotificationCenterDelegate` | class | `Platforms/iOS/AppDelegate.cs` |
| `BaseViewModel` | class | `ViewModels/BaseViewModel.cs` |
| `BuildPushConfiguration` | private method | `Services/PushRegistrationCoordinator.cs` |
| `CreateMauiApp` | static method | `MauiProgram.cs`, `Platforms/*/AppDelegate.cs`, `Platforms/Windows/App.xaml.cs` |
| `CreateNotificationChannel` | private method | `Platforms/Android/AndroidPushNotificationService.cs` |
| `DeleteTokenAsync` | method | push services |
| `DeviceInfoProvider` | class | `Services/DeviceInfoProvider.cs` |
| `DeviceInfoRow` | record | `ViewModels/DeviceInfoViewModel.cs` |
| `DeviceInfoViewModel` | class | `ViewModels/DeviceInfoViewModel.cs` |
| `DummyDeviceRegistrationService` | class | `Services/Api/DummyDeviceRegistrationService.cs` |
| `FirebaseMessageMapper` | static class | `Platforms/Android/FirebaseMessageMapper.cs` |
| `FirebasePushBridge` | static class | `Platforms/Android/FirebasePushBridge.cs` |
| `GetDeviceInfo` | method | `Services/DeviceInfoProvider.cs` |
| `GetDeviceTokenAsync` | method | push services |
| `GetOrCreateInstallationId` | method | `Services/DeviceInfoProvider.cs` |
| `GetPlatform` | private method | `Services/DeviceInfoProvider.cs` |
| `GetPlatformDetails` | partial method | `Platforms/*/DeviceInfoProvider.Platform.cs` |
| `GetPreferencesAsync` | method | `Services/Preferences/NotificationPreferencesService.cs` |
| `GetScreenResolution` | private method | `Services/DeviceInfoProvider.cs` |
| `HandleNotificationIntent` | private method | `Platforms/Android/MainActivity.cs` |
| `HandlePermissionResult` | internal static method | `Platforms/Android/AndroidNotificationPermissionService.cs` |
| `HomeViewModel` | class | `ViewModels/HomeViewModel.cs` |
| `InitializeAsync` | method | push services |
| `IsAppInForeground` | private method | `Platforms/Android/AppFirebaseMessagingService.cs` |
| `LoadAsync` | method | view models and views |
| `Map` | static method | `Platforms/Android/FirebaseMessageMapper.cs`, `Platforms/iOS/AppleNotificationPermissionService.cs` |
| `MapIntent` | static method | `Platforms/Android/FirebaseMessageMapper.cs` |
| `MauiProgram` | static class | `MauiProgram.cs` |
| `NSDictionaryExtensions` | static class | `Platforms/iOS/AppDelegate.cs` |
| `NotificationPreferenceItem` | class | `ViewModels/NotificationPreferenceItem.cs` |
| `NotificationPreferencesDialogService` | class | `Views/NotificationPreferencesDialogService.cs` |
| `NotificationPreferencesPopupPage` | class | `Views/NotificationPreferencesPopupPage.xaml.cs` |
| `NotificationPreferencesService` | class | `Services/Preferences/NotificationPreferencesService.cs` |
| `NotificationPreferencesView` | class | `Views/NotificationPreferencesView.xaml.cs` |
| `NotificationPreferencesViewModel` | class | `ViewModels/NotificationPreferencesViewModel.cs` |
| `NotificationSoftPromptPage` | class | `Views/NotificationSoftPromptPage.xaml.cs` |
| `OnAcceptClicked` / `OnDeclineClicked` / `OnBackButtonPressed` | event handlers / override | `Views/NotificationSoftPromptPage.xaml.cs` |
| `OnCloseClicked` / `OnCloseRequested` / `OnScrimTapped` | event handlers | preference views |
| `OnItemToggled` | private method | `ViewModels/NotificationPreferencesViewModel.cs` |
| `OnMessageReceived` | event handler | push services and `HomeViewModel` |
| `OnNewToken` | override | `Platforms/Android/AppFirebaseMessagingService.cs` |
| `OnNotificationTapped` | event handler | push services and `HomeViewModel` |
| `OnRequestPermissionsResult` | override | `Platforms/Android/MainActivity.cs` |
| `OnTokenRefreshed` | event handler | `Services/PushRegistrationCoordinator.cs` and push services |
| `PersistToBackendAsync` | private method | `Services/Preferences/NotificationPreferencesService.cs` |
| `PlatformDeviceDetails` | record struct | `Services/DeviceInfoProvider.cs` |
| `PopulateRows` | private method | `ViewModels/DeviceInfoViewModel.cs` |
| `PushPermissionFlowResult` | record | `Services/PushPermissionFlowService.cs` |
| `PushPermissionFlowService` | class | `Services/PushPermissionFlowService.cs` |
| `PushRegistrationCoordinator` | class | `Services/PushRegistrationCoordinator.cs` |
| `RaiseMessageReceived` / `RaiseNotificationTapped` / `RaiseTokenRefreshed` | static methods | bridge classes |
| `RegisterAsync` | method | `Services/PushRegistrationCoordinator.cs`, `ViewModels/DeviceInfoViewModel.cs` |
| `RegisterDeviceAsync` | method | `Services/Api/DummyDeviceRegistrationService.cs` |
| `RegisterForRemoteNotifications` | call | `Platforms/iOS/ApplePushNotificationService.cs` |
| `RegisteredForRemoteNotifications` | exported method | `Platforms/iOS/AppDelegate.cs` |
| `RegisterWithBackendAsync` | private method | `Services/PushRegistrationCoordinator.cs` |
| `RequestAsync` | method | permission services |
| `RequestPermissionAsync` | private method | `ViewModels/HomeViewModel.cs` |
| `RequestPermissionWithSoftPromptAsync` | method | `Services/PushPermissionFlowService.cs` |
| `SaveInstallationAsync` | method | coordinator and registration service |
| `SaveNotificationPreferencesAsync` | method | registration service |
| `SetAllAsync` / `SetEnabledAsync` | method | preferences service |
| `SetDeviceToken` | static method | `Platforms/iOS/ApplePushBridge.cs` |
| `SetEnabledSilently` | method | `ViewModels/NotificationPreferenceItem.cs` |
| `SetProperty` | protected method | `ViewModels/BaseViewModel.cs` |
| `ShowAsync` | method | `Views/NotificationPreferencesDialogService.cs` |
| `ShowSoftPromptAsync` | private method | `Services/PushPermissionFlowService.cs` |
| `SimulateNetworkAsync` | private method | `Services/Api/DummyDeviceRegistrationService.cs` |
| `SynchronizeSubscriptionsAsync` | method | preferences service |
| `Sysctl` / `sysctlbyname` | private method / DllImport | `Platforms/iOS/DeviceInfoProvider.Platform.cs` |
| `ToDictionary` | extension method | `Platforms/iOS/AppDelegate.cs` |
| `ToggleAllAsync` | private method | `ViewModels/NotificationPreferencesViewModel.cs` |
| `TopicName` | static method | `Platforms/Android/AndroidPushNotificationService.cs` |
| `UnregisterAsync` | method | coordinator and `DeviceInfoViewModel` |
| `UnregisterDeviceAsync` | method | registration service |
| `WillPresentNotification` / `DidReceiveNotificationResponse` | overrides | `Platforms/iOS/AppDelegate.cs` |

---

## Appendix B — notification topic catalog

| Enum value | FCM topic name (Android) | Default enabled | Display name |
| --- | --- | --- | --- |
| `Announcements` | `announcements` | No | Product announcements |
| `AccountAlerts` | `accountalerts` | Yes | Account alerts |
| `OrderUpdates` | `orderupdates` | Yes | Order updates |
| `Promotions` | `promotions` | No | Promotions |
| `Tips` | `tips` | No | Tips and guides |
| `WeeklyDigest` | `weeklydigest` | No | Weekly digest |

On Android the FCM topic name is the enum name lowercased via
`AndroidPushNotificationService.TopicName`. On iOS the topic is carried as a custom key in
the APNs payload and enforced by the backend.

---

This document was generated by an AI agent (OpenHands) on behalf of the repository owner.
