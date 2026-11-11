namespace LGC.PIT.Plugin.Common.Configs;

public sealed record ApiSettings()
{    
    public string EmployeeUrl { get; set; }
    public string TokenURL { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string GrantType { get; set; } = AppConstants.DEFAULT_GRANT_TYPE;
}