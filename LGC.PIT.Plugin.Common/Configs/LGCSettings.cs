using System;

namespace LGC.PIT.Plugin.Common.Configs;

public sealed class LGCSettings
{    
    public Guid CardholderGroupId { get; set; }
    public ApiSettings ApiSettings { get; set; }
}
