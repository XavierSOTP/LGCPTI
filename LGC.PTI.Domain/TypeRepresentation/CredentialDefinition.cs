using SoftSolute.Plugin.Common.EntityDefinitions;

namespace LGC.PTI.Domain.TypeRepresentation;

/// <summary>
/// Defines the Genetec Credential entity configuration required by the PTI integration.
/// </summary>
/// <remarks>
/// This definition identifies the credential custom fields used to determine whether
/// a credential participates in PTI synchronization.
/// </remarks>
public sealed class CredentialDefinition : EntityDefinition<CredentialDefinition, Credential>
{
    #region Fields

    /// <summary>
    /// Defines the custom-field group that contains PTI-related Credential fields.
    /// </summary>
    private const string GROUP = "PLUGIN EXTERN";

    #endregion Fields

    #region Properties

    /// <summary>
    /// Gets the custom-field definition that indicates whether the credential is enabled
    /// for use by the PTI integration.
    /// </summary>
    /// <remarks>
    /// The field is configured for <see cref="EntityType.Credential"/>, uses the
    /// <c>USED PLUGIN EXT</c> field name, stores Boolean values, and belongs to the
    /// configured custom-field group.
    /// </remarks>
    public SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO IsUsedCredential { get; } = new SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO(EntityType.Credential, "USED PLUGIN EXT", CustomFieldValueType.Boolean) { GroupName = GROUP };

    #endregion Properties

    #region Methods

    /// <summary>
    /// Creates a new <see cref="CredentialDefinition"/> instance.
    /// </summary>
    /// <returns>
    /// A new <see cref="CredentialDefinition"/> instance.
    /// </returns>
    public static CredentialDefinition New() => new();

    #endregion Methods
}