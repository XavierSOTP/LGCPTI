using LGC.PIT.Plugin.Common.Configs;

namespace LGC.PTI.App.Plugin.Server;

public static class PluginConfigMapper
{
    #region Public Methods
    public static LGCPluginExeConfig ToAppConfig(LGCPluginConfig pluginConfig, IEngine engine, NotEmptyGuid LocalpluginGuid, NotEmptyString pluginLocation, string sdkCertificate)
    {
        var EndpointDetails =
            PluginConfigMapperHelper.ExtractEndpointDetails(
                engine.OriginalDirectoryEndpoint);

        ServerLog.Info(
            $"Directory: '{EndpointDetails.Directory}'");

        //ServerLog.Info(
        //    $"SCLoginUsername empty: " +
        //    $"{string.IsNullOrWhiteSpace(pluginConfig.SCLoginUsername)}");

        //ServerLog.Info(
        //    $"SCLoginPassword empty: " +
        //    $"{string.IsNullOrWhiteSpace(pluginConfig.SCLoginPassword)}");
        return new LGCPluginExeConfig
        {
            TransactSettings = new LGCExeSettings
            {
                LocalPluginGuid = LocalpluginGuid,
                PluginFolderPath = pluginLocation,
                Debug = true,
                LGCSettings = new LGCSettings
                {
                    CardholderGroupId = pluginConfig.CardholderGroupId,
                    ApiSettings = new ApiSettings
                    {
                        EmployeeUrl = pluginConfig.EmployeeUrl,
                        TokenURL = pluginConfig.TokenURL,
                        ClientId = pluginConfig.ClientId,
                        ClientSecret = pluginConfig.ClientSecret,
                        GrantType = pluginConfig.GrantType
                    }
                },

            },
            SdkSettings = new PluginSdkSettings
            {
                SdkLocation = "dummy",
                SCLocation = "dummy",
                Directory = "dummy",
                User = "dummy",
                Password = "dummy"
            },
            //SCLoginData = new PluginSCLoginData(
            //    EndpointDetails.Directory,
            //    pluginConfig.SCLoginUsername,
            //    pluginConfig.SCLoginPassword)
            //{
            //    Certificate = sdkCertificate,
            //    Port = EndpointDetails.Port,
            //    Retry = 5
            //}
        };
    }

    public static class PluginConfigMapperHelper
    {
        #region Public Methods

        /// <summary>
        /// Converts a "HH:mm" string to a Quartz-compatible CRON expression (e.g., "0 10 0 * * ?")
        /// </summary>
        public static string ConvertToCron(NotEmptyString timeString)
        {
            PluginTodoCode.Instance.RelocateTo<LanguageExt.Unit>(destinationProject: "Plugin.Common and  SS.Common.Utilities", "Create a class or specialized method");
            if (!TimeSpan.TryParseExact(timeString, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan time))
                throw new FormatException($"Invalid time format: '{timeString}'. Expected HH:mm.");

            int hour = time.Hours;
            int minute = time.Minutes;

            // Quartz.NET format: second minute hour day-of-month month day-of-week (optional year)
            return $"0 {minute} {hour} * * ?";
        }

        /// <summary>
        /// Parses an endpoint string like "127.0.0.1:5500" or "softsolute.local"
        /// into a ParsedEndpoint record (Directory and Port).
        /// </summary>

        public static ParsedEndpoint ExtractEndpointDetails(NotEmptyString endpoint)
        {
            var parts = endpoint.Value.Split(':');

            return parts.Length == 2 && int.TryParse(parts[1], out int port)
                ? new ParsedEndpoint(parts[0], port)
                : new ParsedEndpoint(endpoint, null);
        }

        #endregion Public Methods
    }

    #endregion Public Methods
}