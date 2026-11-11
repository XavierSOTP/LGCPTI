namespace LGC.PIT.Plugin.Common.Database.Entities;

public class Configuration : BaseEntity<int>
{
    public string Key { get; set; }
    public string Value { get; set; }
}
