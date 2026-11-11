using SoftSolute.Plugin.Common.EntityDefinitions;

namespace LGC.PTI.Domain.TypeRepresentation;

/// <summary>
/// Defines the Genetec Cardholder entity configuration required by the PTI integration.
/// </summary>
/// <remarks>
/// This definition identifies the custom fields that must be available on
/// <see cref="Cardholder"/> entities for PTI processing.
/// </remarks>
public sealed class CardholderDefinition : EntityDefinition<CardholderDefinition, Cardholder>
{
    #region Fields

    /// <summary>
    /// Defines the custom-field group that contains PTI-related Cardholder fields.
    /// </summary>
    private const string GROUP = "PLUGIN EXTERN";

    #endregion Fields

    #region Properties

    /// <summary>
    /// Gets the custom-field definition that contains the Cardholder department or service value.
    /// </summary>
    /// <remarks>
    /// The field is configured for <see cref="EntityType.Cardholder"/>, uses the
    /// <c>Service</c> field name, stores text values, and belongs to the configured custom-field group.
    /// </remarks>
    public SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO Department { get; } = new SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO(EntityType.Cardholder, "Service", CustomFieldValueType.Text) { GroupName = GROUP };

    #endregion Properties

    #region Methods

    /// <summary>
    /// Creates a new <see cref="CardholderDefinition"/> instance.
    /// </summary>
    /// <returns>
    /// A new <see cref="CardholderDefinition"/> instance.
    /// </returns>
    public static CardholderDefinition New() => new();

    #endregion Methods
}