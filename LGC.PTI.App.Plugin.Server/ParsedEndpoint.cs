namespace LGC.PTI.App.Plugin.Server;

public struct ParsedEndpoint
{
    public string Directory { get; }
    public int? Port { get; }

    public ParsedEndpoint(string directory, int? port)
    {
        Directory = directory;
        Port = port;
    }
}