using LanguageExt;
using Microsoft.Extensions.Logging;
using R3;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.LanguageExtSS;
using SoftSolute.Licensing;
using SoftSolute.Logging;
using ILicenseExpirator = SoftSolute.Licensing.ILicenseExpirator;
using INotifiableLicExpiration = SoftSolute.Licensing.INotifiableLicExpiration;
using License = SoftSolute.Licensing.License;
using LicenseCloseToExpireEventArgs = SoftSolute.Licensing.LicenseCloseToExpireEventArgs;
using LicenseSystemInfo = SoftSolute.Licensing.SystemInfo;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using MicrosoftLogger = Microsoft.Extensions.Logging.ILogger;
using Unit = LanguageExt.Unit;
namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Manages the initial loading and validation of a plugin license.
/// Provides an effect-based interface for license validation without monitoring.
/// </summary>
public interface IUnInitLicenseManager : IDisposable
{
    /// <summary>
    /// Loads and validates the license for a plugin.
    /// </summary>
    /// <param name="cert">Certificate used to validate the license.</param>
    /// <param name="sdkVersion">SDK version supported by the plugin.</param>
    /// <param name="pluginName">Plugin name.</param>
    /// <param name="pluginInstanceId">Plugin instance identifier.</param>
    /// <param name="licenseFolder">Folder that contains the license file.</param>
    /// <returns>
    /// An effect that returns the initialized license manager when validation succeeds,
    /// or a failure when the license cannot be loaded or validated.
    /// </returns>
    Eff<ILicenseManagerInitialized> TryLoadLicense(
        NotEmptySeq<byte> cert,
        Version sdkVersion,
        NotEmptyString pluginName,
        IntGtZero pluginInstanceId,
        NotEmptyString licenseFolder);
}

/// <summary>
/// Exposes the validated license and notifications for licenses that are close to expiring.
/// </summary>
public interface ILicenseManagerInitialized
{
    /// <summary>
    /// Gets an observable that emits license expiration warnings when the license is close to expiring.
    /// </summary>
    Observable<LicenseCloseToExpireEventArgs> OnExpiration { get; }

    /// <summary>
    /// Gets the validated license object containing license information and expiration details.
    /// </summary>
    License License { get; }
}

/// <summary>
/// Loads, validates, and monitors a plugin license.
/// Monitors license expiration and notifies subscribers when the license is running out.
/// Implements both uninitialized and initialized license manager interfaces.
/// </summary>
public sealed class PluginLicenseManager : IUnInitLicenseManager, ILicenseManagerInitialized
{
    /// <summary>
    /// Interval in milliseconds for checking license expiration status.
    /// </summary>
    private const int LicenseCheckIntervalMilliseconds = 60_000;

    /// <summary>
    /// Standard filename for the license file.
    /// </summary>
    private const string LicenseFileName = "License.lic";

    /// <summary>
    /// Logger message for general errors.
    /// </summary>
    private static readonly Action<MicrosoftLogger, string, Exception?> LogAnyError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            1001,
            "{Error}");

    /// <summary>
    /// Logger message for disposal errors during license expiration.
    /// </summary>
    private static readonly Action<MicrosoftLogger, string, Exception?> LogExpirationError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            1000,
            "Error disposing :{Error}");

    /// <summary>
    /// Logger message for validation key errors.
    /// </summary>
    private static readonly Action<MicrosoftLogger, string, Exception?> LogValidationKeyError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            1002,
            "ValidationKey: {Key}");

    /// <summary>
    /// The license expiration monitor service.
    /// </summary>
    private readonly ILicenseExpirator _licenseExpirator;

    /// <summary>
    /// The logger for diagnostic messages.
    /// </summary>
    private readonly MicrosoftLogger _logger;

    /// <summary>
    /// The current license expiration notifier.
    /// </summary>
    private INotifiableLicExpiration? _licenseExpirationNotifier;

    /// <summary>
    /// Flag indicating whether the manager has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLicenseManager"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="licenseExpirator">Service for monitoring license expiration.</param>
    private PluginLicenseManager(
        MicrosoftLogger logger,
        ILicenseExpirator licenseExpirator)
    {
        _logger = logger;
        _licenseExpirator = licenseExpirator;
    }

    /// <summary>
    /// Gets an observable that emits license expiration warnings.
    /// </summary>
    public Observable<LicenseCloseToExpireEventArgs> OnExpiration { get; private set; } =
        Observable.Empty<LicenseCloseToExpireEventArgs>();

    /// <summary>
    /// Gets the current validated license.
    /// </summary>
    public License License { get; private set; } = null!;

    /// <summary>
    /// Creates a new instance of the license manager.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="licenseExpirator">Service for monitoring license expiration.</param>
    /// <returns>A new uninitialized license manager instance.</returns>
    public static IUnInitLicenseManager Create(
        ILogger<PluginLicenseManager> logger,
        ILicenseExpirator licenseExpirator) =>
        new PluginLicenseManager(logger, licenseExpirator);

    /// <summary>
    /// Attempts to load and validate a license for the plugin.
    /// Validates license file existence, format, and compatibility with the system.
    /// </summary>
    /// <param name="cert">Certificate bytes for license validation.</param>
    /// <param name="sdkVersion">SDK version to validate against.</param>
    /// <param name="pluginName">Name of the plugin.</param>
    /// <param name="pluginInstanceId">Instance identifier for the plugin.</param>
    /// <param name="licenseFolder">Path to the folder containing the license file.</param>
    /// <returns>An effect that returns this manager as an initialized license manager, or fails with validation errors.</returns>
    public Eff<ILicenseManagerInitialized> TryLoadLicense(
        NotEmptySeq<byte> cert,
        Version sdkVersion,
        NotEmptyString pluginName,
        IntGtZero pluginInstanceId,
        NotEmptyString licenseFolder)
    {
        var certificate = cert.Value.ToArray();
        var licenseFilePath = Path.Combine(licenseFolder, LicenseFileName);

        return
            from systemId in LicenseHandler.GetSystemId(
                pluginName.Value,
                pluginInstanceId)
            from _fileValidated in ValidateLicenseFileExists(
                licenseFilePath,
                systemId)
            from license in LicenseHandler.TryReadLicenseFile(
                licenseFilePath,
                certificate)
            from validatedLicense in TryValidateLicense(
                license,
                pluginName,
                sdkVersion,
                pluginInstanceId)
            from _licenseAssigned in SetCurrentLicense(validatedLicense)
            select (ILicenseManagerInitialized)this;
    }

    /// <summary>
    /// Releases all resources used by the license manager.
    /// Stops license expiration monitoring and disposes related resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Validates a loaded license against version and system requirements.
    /// Checks SDK version compatibility and license validity, then starts expiration monitoring.
    /// </summary>
    /// <param name="license">The license to validate.</param>
    /// <param name="pluginName">Name of the plugin.</param>
    /// <param name="version">SDK version to validate against.</param>
    /// <param name="pluginInstanceId">Instance identifier for the plugin.</param>
    /// <returns>An effect that returns the validated license or fails with validation errors.</returns>
    internal Eff<License> TryValidateLicense(
        License license,
        NotEmptyString pluginName,
        Version version,
        IntGtZero pluginInstanceId)
    {
        var localSystemInfo =
            from systemId in LicenseHandler.GetSystemId(
                pluginName.Value,
                pluginInstanceId)
            select new LicenseSystemInfo(systemId, version);

        return
            from systemInfo in localSystemInfo
            from _versionValidated in LicenseHandler
                .ValidateSCVersion(
                    license.SCVersion,
                    systemInfo.SCVersion.ToVersion())
                .OnFail(_ => LogInvalidVersionError(
                    license.SCVersion.ToString(),
                    systemInfo))
            from validatedLicense in LicenseHandler
                .ValidateLicense(license, systemInfo)
                .OnFail(() => LogLicenseError(systemInfo.SystemId))
            from _expirationMonitorStarted in OnLicenseValidation(validatedLicense)
            select validatedLicense;
    }

    /// <summary>
    /// Validates that the license file exists and is not empty.
    /// </summary>
    /// <param name="licenseFilePath">The path to the license file.</param>
    /// <param name="systemId">The system identifier for error reporting.</param>
    /// <returns>An effect that returns unit on success or fails if the file does not exist or is empty.</returns>
    private Eff<Unit> ValidateLicenseFileExists(
        string licenseFilePath,
        string systemId)
    {
        return Prelude.Eff(() =>
        {
            var licenseFile = new FileInfo(licenseFilePath);

            if (licenseFile.Exists && licenseFile.Length > 0)
            {
                return Prelude.unit;
            }

            LogLicenseError(systemId);

            throw new FileNotFoundException(
                $"License file does not exist or is empty: {licenseFilePath}",
                licenseFilePath);
        });
    }

    /// <summary>
    /// Sets the current license for this manager.
    /// </summary>
    /// <param name="license">The license to set.</param>
    /// <returns>An effect that returns unit on success.</returns>
    private Eff<Unit> SetCurrentLicense(License license)
    {
        return Prelude.Eff(() =>
        {
            License = license;
            return Prelude.unit;
        });
    }

    /// <summary>
    /// Logs license validation error messages.
    /// </summary>
    /// <param name="systemId">The system identifier for which validation failed.</param>
    private void LogLicenseError(string systemId)
    {
        LogAnyError(_logger, "Request a new license to SoftSolute", null);
        LogValidationKeyError(_logger, systemId, null);
    }

    /// <summary>
    /// Logs SDK version mismatch errors with detailed information.
    /// </summary>
    /// <param name="pluginVersion">The version the plugin was built with.</param>
    /// <param name="systemInfo">The system information containing the running SDK version.</param>
    private void LogInvalidVersionError(string pluginVersion, LicenseSystemInfo systemInfo)
    {
        LogAnyError(_logger, $"System ID: {systemInfo.SystemId}", null);
        LogAnyError(_logger, "Error: SDK version mismatch detected.", null);
        LogAnyError(
            _logger,
            $"The plugin was built against SDK version {pluginVersion}, " +
            $"but the system is running SDK version {systemInfo.SCVersion}.",
            null);
        LogAnyError(
            _logger,
            "Please rebuild or update the plugin to match the system SDK version.",
            null);
    }

    /// <summary>
    /// Handles license validation completion and starts expiration monitoring.
    /// Starts the license expiration notifier if the license is not permanent.
    /// </summary>
    /// <param name="license">The validated license.</param>
    /// <returns>An effect that returns unit on success.</returns>
    private Eff<Unit> OnLicenseValidation(License license)
    {
        return Prelude.Eff(() =>
        {
            _logger.Info("License validated.");

            if (IsDemoLicense(license))
            {
                _logger.Info(
                    $"This is a Demo License that will expire on: {license.ExpirationDate:O}.");
            }

            OnExpiration = StartLicenseExpirator(license.ExpirationDate);

            return Prelude.unit;
        });
    }

    /// <summary>
    /// Determines if a license is a demo license.
    /// </summary>
    /// <param name="license">The license to check.</param>
    /// <returns>True if the license type is "Demo"; otherwise false.</returns>
    private static bool IsDemoLicense(License license)
    {
        return string.Equals(
            license.LicenseType,
            nameof(LicenseType.Demo),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Starts the license expiration monitoring service.
    /// Creates an observable that emits events when the license is close to expiring or has expired.
    /// </summary>
    /// <param name="expiration">The license expiration date and time.</param>
    /// <returns>An observable that emits license expiration notifications.</returns>
    private Observable<LicenseCloseToExpireEventArgs> StartLicenseExpirator(
        DateTime expiration)
    {
        _logger.Info(
            $"Starting license expiration monitor. Expiration date: {expiration:O}. " +
            $"Check interval: {LicenseCheckIntervalMilliseconds} ms.");

        var notifier = _licenseExpirator
            .SetExpiration(
                expiration,
                LicenseCheckIntervalMilliseconds,
                error =>
                {
                    LogAnyError(
                        _logger,
                        $"License expired. Expiration date: {expiration:O}. Error: {error}",
                        null);

                    Environment.Exit(20);

                    return Prelude.unit;
                })
            .Start();

        _licenseExpirationNotifier = notifier;

        return Observable.FromEvent<
            EventHandler<LicenseCloseToExpireEventArgs>,
            LicenseCloseToExpireEventArgs>(
            handler => (_, args) => handler(args),
            handler => notifier.LicenseCloseToExpire += handler,
            handler => notifier.LicenseCloseToExpire -= handler);
    }

    /// <summary>
    /// Releases managed resources held by this manager.
    /// </summary>
    /// <param name="disposing">True if called from Dispose; false if called from finalizer.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _ = Prelude.Eff(() =>
            {
                _licenseExpirationNotifier?.Dispose();
                return Prelude.unit;
            })
                .OnFail(error =>
                    LogExpirationError(
                        _logger,
                        error.Message,
                        null))
                .Run();
        }

        _disposed = true;
    }
}
