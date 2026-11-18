namespace LGC.PIT.Plugin.Common.Configs;

/// <summary>Configuration for the PTI GraphQL employee API.</summary>
public sealed class GraphQLSettings
{
    public const string DefaultEndpoint = "https://us-central1-storeroom-mgmnt-suite.cloudfunctions.net/data";
    public string Endpoint { get; set; } = DefaultEndpoint;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
