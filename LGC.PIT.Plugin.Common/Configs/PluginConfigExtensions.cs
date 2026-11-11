using System.Collections.Generic;
using System.Linq;

namespace LGC.PIT.Plugin.Common.Configs;

public static class PluginConfigExtensions
{
    #region Public Methods

    public static bool IsPluginConfigValid(this LGCPluginConfig config, out List<string> errors)
    {
        var validator = new LGCPluginConfigValidator();
        var result = validator.Validate(config);

        errors = result.IsValid
            ? []
            : result.Errors.Select(e => e.ErrorMessage).ToList();

        return result.IsValid;
    }

    #endregion Public Methods
}
