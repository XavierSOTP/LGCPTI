using LGC.PIT.Plugin.Common.Database;
using SoftSolute.Plugin.Common;
using System;

namespace LGC.PIT.Plugin.Common.Configs;

public sealed class LGCPluginConfig : BasePluginConfig<LGCPluginConfig>
{
    #region Properties

    //[SensitiveData]
    //public string SCLoginPassword { get; set; } = string.Empty;

    //public string SCLoginUsername { get; set; } = string.Empty;

    public string EmployeeUrl { get; set; }

    [SensitiveData]
    public string TokenURL { get; set; }
    public string GrantType { get; set; } = AppConstants.DEFAULT_GRANT_TYPE;
    [SensitiveData]
    public string ClientId { get; set; }
    [SensitiveData]
    public string ClientSecret { get; set; }
    public Guid CardholderGroupId { get; set; }

    #endregion Properties

    #region Public Methods

    public LGCPluginConfig Clone()
    {
        return new LGCPluginConfig
        {
            //SCLoginUsername = this.SCLoginUsername,
            //SCLoginPassword = this.SCLoginPassword,
            EmployeeUrl = this.EmployeeUrl,
            TokenURL = this.TokenURL,
            GrantType = this.GrantType,
            ClientId = this.ClientId,
            ClientSecret = this.ClientSecret,
            CardholderGroupId = this.CardholderGroupId
        };
    }

    #endregion Public Methods
}
