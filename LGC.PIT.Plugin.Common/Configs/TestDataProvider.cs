using SoftSolute.Plugin.Common.Configurations;
using SoftSolute.Plugin.Common.Helpers;
using System;
using System.IO;
using License = SoftSolute.Plugin.Common.Configurations.License;

namespace LGC.PIT.Plugin.Common.Configs;

[Flags]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2346:Flags enumerations zero-value members should be named \"None\"", Justification = "<Pending>")]
public enum PartType
{
    Development = 0,
    Propietary = 2
}

public static partial class TestDataProvider
{
    #region Private Methods

    // Development
    public static PartModel Dev => new() { Name = "Dev", PartType = PartType.Development, Certificate = "KxsD11z743Hf5Gq9mv3+5ekxzemlCiUXkTFY5ba1NOGcLCmGstt2n0zYE9NsNimv" };
    // Alter
    //public static PartModel Dev => new() { Name = "Dev", PartType = PartType.Development, Certificate = "vryUtfbgLbxPLMkriGlU0cJCQ83baA2z2DiO5BFaJT9yaIe6Vv7OZl4t1ZZTDSWF" };
    // Vantage
    //public static PartModel Dev => new() { Name = "Dev", PartType = PartType.Development, Certificate = "zCMQpUkK1dny3y1mdPKOECA4lkSGRKWO4IIbFIgkHDyDMkavGQDEO4OD/81C/0hT" };

    public static VersionSS SC513 => new(5, 13);

    public static class HardcodedPluginConfigFactory
    {
        #region Public Methods

        private const string DefaultQueueSubfolder = "queue";

        public static void CompleteNonMappedFields(LGCPluginExeConfig config)
        {
            if (config == null) return;

            // License
            config.License ??= new License("SCPIM", "44YF3K-1IUORV6-H0EM20-2L8OAR-1000", SC513)
                .SetExpiration(DateTime.Now.AddDays(180));
            config.License.GenetecCertificate ??= Dev.Certificate;

            //config.SapSettings.SapQueueConfig.Folder = CreateDefaultQueueFolder();
        }

        /// <summary>
        /// Automatically creates the queue folder within the application base directory.
        /// </summary>
        /// <returns>The absolute path to the queue folder.</returns>
        public static string CreateDefaultQueueFolder()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var queueFolderPath = Path.Combine(baseDirectory, DefaultQueueSubfolder);

            if (!Directory.Exists(queueFolderPath))
            {
                Directory.CreateDirectory(queueFolderPath);
            }

            return queueFolderPath;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "<Pending>")]
        public static TExecConfiguration CreatePluginExeConfig<TExecConfiguration, TExeSettings>()
            where TExecConfiguration : IPluginExeConfig<TExeSettings>, new()
            where TExeSettings : IConfigurableAppSettings, new()
        {
            var systemId = "44YF3K-1IUORV6-H0EM20-2L8OAR-1000";
            var scVersion = SC513;
            var expirationDate = DateTime.Now.AddDays(180);

            var license = new License("SCPIM", systemId, scVersion)
            {
                GenetecCertificate = Dev.Certificate,
            };
            license = license.SetExpiration(expirationDate);

            string internalOutputFolder = Path.Combine(Path.GetTempPath(), "Files");
            string internalFolder = Path.Combine(Path.GetTempPath(), "Queues");
            string resourcesFolder = Path.Combine(Path.GetTempPath(), "Resources");

            Directory.CreateDirectory(internalOutputFolder);
            Directory.CreateDirectory(internalFolder);
            Directory.CreateDirectory(resourcesFolder);

            var configurableSettings = (TExeSettings)(IConfigurableAppSettings)new LGCExeSettings
            {
                Debug = true,
                ConnectionString = "Server=(local)\\SQLEXPRESS;Database=PluginDatabase;Trusted_Connection=True;TrustServerCertificate=yes;",
                //ConnectionString = "Server=SS-DEV-SRV-04\\SQLEXPRESS;Database=PluginDatabase;Trusted_Connection=True;TrustServerCertificate=yes;",

                LGCSettings = new LGCSettings
                {
                    ApiSettings = new ApiSettings
                    {
                        EmployeeUrl = "https://api.iqtechportal.com/api/integration/Employee?page=1&pageSize=500",
                        TokenURL = "https://account.iqtechportal.com/connect/token",
                        ClientId = "QSL Grande-Anse",
                        ClientSecret = "iWoRsb",
                        GrantType = AppConstants.DEFAULT_GRANT_TYPE,
                    },
                },
            };

            configurableSettings.SetResourceFolder(resourcesFolder);

            return new TExecConfiguration
            {
                License = license,
                SdkSettings = new PluginSdkSettings
                {
                    SdkLocation = @"C:\Program Files (x86)\Genetec Security Center 5.13",
                    SCLocation = @"C:\Program Files (x86)\Genetec Security Center 5.13",
                    Directory = "softsolute.hopto.org:9010",
                    User = "admin",
                    Password = "b068I4lX/csCZ1EDyvDxBULnoP8R5AhOCqrLC4UfpAxHwotVGcbIAmxW4/Ijgn+VbGv2HT6p4bpBGLSJu7rcrffHL+WPqxcYk34wd/QLCemPQsEa5dmqMAhvV+YCZpKX1mhKEUkjkfqBglo+3YNs4NfGdpHfz4LvxoY8kJ3MnwJTRlOkHmj4I7wLxJrPQzG2yJs4CYNx4xY6eCc8fdYSND5weN+hKg6TvaVTrgyLdlmLD4/nFDh4ejseJDbTek70vd0+QZPhM+TQ7dyluUNlWHTXghGM43wKRglJq3vjAo9/xzFiI2M0KXUcRp43dyvWfbG+3ZujM8kcjzoWo/9I6Q=="
                },
                //SCLoginData = new PluginSCLoginData("127.0.0.1", "Admin", "#vSwNPs#[3Nd8^qL(r=")
                SCLoginData = new PluginSCLoginData("127.0.0.1", "admin", "G3netec!?")
                {
                    Certificate = Dev.Certificate,
                    //Port = 9010,
                    //Port = 9009,
                    Retry = 5
                },
                TransactSettings = configurableSettings,
            };
        }

        #endregion Public Methods
    }

    #endregion Private Methods
}

public sealed class PartModel
{
    #region Public Constructors

    public PartModel()
    {
        PartType = PartType.Propietary;
    }

    #endregion Public Constructors

    #region Properties

    /// <summary>
    ///
    /// </summary>
    public string Certificate { get; set; }

    /// <summary>
    /// Friendly name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Genetec part number
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    ///
    /// </summary>
    public PartType PartType { get; set; }

    #endregion Properties
}