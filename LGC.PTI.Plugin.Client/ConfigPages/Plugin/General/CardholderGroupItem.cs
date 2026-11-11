namespace LGC.PTI.Plugin.Client.ConfigPages.Plugin.General;

/// <summary>
/// Represents a cardholder group item used by the plugin configuration UI.
/// </summary>
public sealed class CardholderGroupItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the cardholder group.
    /// </summary>
    public Guid Guid { get; set; }

    /// <summary>
    /// Gets or sets the display name of the cardholder group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the associated cardholder group entity.
    /// </summary>
    public CardholderGroup? Entity { get; set; }
}