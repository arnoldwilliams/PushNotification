using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PushNotification.App.Models;
using PushNotification.App.Services;
using PushNotification.App.Services.Notifications;

namespace PushNotification.App.ViewModels;

/// <summary>
/// Shows the installation details that are registered with the backend and exposes
/// the register/save/unregister actions.
/// </summary>
public sealed class DeviceInfoViewModel : BaseViewModel
{
    private readonly IPushRegistrationCoordinator _coordinator;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly INotificationPermissionService _permissionService;
    private readonly ILogger<DeviceInfoViewModel> _logger;

    public DeviceInfoViewModel(
        IPushRegistrationCoordinator coordinator,
        IDeviceInfoProvider deviceInfoProvider,
        INotificationPermissionService permissionService,
        ILogger<DeviceInfoViewModel> logger)
    {
        _coordinator = coordinator;
        _deviceInfoProvider = deviceInfoProvider;
        _permissionService = permissionService;
        _logger = logger;

        RegisterCommand = new Command(async () => await RegisterAsync());
        SaveInstallationCommand = new Command(async () => await SaveInstallationAsync());
        UnregisterCommand = new Command(async () => await UnregisterAsync());
    }

    public ObservableCollection<DeviceInfoRow> Rows { get; } = new();

    public ICommand RegisterCommand { get; }

    public ICommand SaveInstallationCommand { get; }

    public ICommand UnregisterCommand { get; }

    private string _permissionStatus = "Unknown";

    public string PermissionStatus
    {
        get => _permissionStatus;
        private set => SetProperty(ref _permissionStatus, value);
    }

    public async Task LoadAsync()
    {
        try
        {
            var installation = _deviceInfoProvider.GetDeviceInfo();
            installation.DeviceToken = _coordinator.CurrentDeviceToken ?? string.Empty;
            PopulateRows(installation);

            var status = await _permissionService.GetStatusAsync().ConfigureAwait(true);
            PermissionStatus = status.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load device information.");
            StatusMessage = "Could not load device information.";
        }
    }

    private async Task RegisterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Registering device...";

            var result = await _coordinator.RegisterAsync().ConfigureAwait(true);
            StatusMessage = result.Success
                ? $"Registered: {result.Message}"
                : $"Registration incomplete: {result.Message}";

            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device registration failed.");
            StatusMessage = "Registration failed. See logs for details.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveInstallationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Saving installation...";

            var result = await _coordinator.SaveInstallationAsync().ConfigureAwait(true);
            StatusMessage = result.Success
                ? $"Installation saved: {result.Message}"
                : $"Save failed: {result.Message}";

            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving the installation failed.");
            StatusMessage = "Could not save the installation.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UnregisterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Unregistering device...";

            await _coordinator.UnregisterAsync().ConfigureAwait(true);
            StatusMessage = "Device unregistered.";
            await LoadAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unregistering the device failed.");
            StatusMessage = "Could not unregister the device.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PopulateRows(DeviceInstallation installation)
    {
        Rows.Clear();
        Add("Installation Id", installation.InstallationId);
        Add("Device Token", string.IsNullOrEmpty(installation.DeviceToken) ? "(not available yet)" : installation.DeviceToken);
        Add("Platform", installation.PlatformName);
        Add("Model", installation.Model);
        Add("Manufacturer", installation.Manufacturer);
        Add("Device Name", installation.DeviceName);
        Add("OS Version", installation.OsVersion);
        Add("App Version", $"{installation.AppVersion} ({installation.AppBuild})");
        Add("Bundle Id", installation.BundleId);
        Add("Locale", installation.Locale);
        Add("Language", installation.Language);
        Add("Time Zone", installation.TimeZone);
        Add("Screen", $"{installation.ScreenResolution} @ {installation.ScreenDensity:0.##}x");
        Add("Device Type", installation.DeviceType);
        Add("Emulator", installation.IsEmulator ? "Yes" : "No");
    }

    private void Add(string label, string value) => Rows.Add(new DeviceInfoRow(label, value));
}

/// <summary>A label/value pair rendered in the device information list.</summary>
public sealed record DeviceInfoRow(string Label, string Value);
