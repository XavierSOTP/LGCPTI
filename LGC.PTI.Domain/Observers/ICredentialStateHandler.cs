
namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Handles credential state transitions and custom field changes in the system.
/// Manages the relationship between credentials and cardholder custom fields.
/// </summary>
public interface ICredentialStateHandler
{
    /// <summary>
    /// Called when custom fields are changed on a credential entity.
    /// </summary>
    /// <param name="sender">The source object that triggered the event.</param>
    /// <param name="e">Event arguments containing change information.</param>
    /// <param name="cardholder">The cardholder associated with the credential.</param>
    void Credential_CustomFieldsChanged(object sender, EventArgs e, Cardholder cardholder);

    /// <summary>
    /// Called when custom fields are changed on an entity.
    /// </summary>
    /// <param name="sender">The source object that triggered the event.</param>
    /// <param name="e">Event arguments containing change information.</param>
    /// <param name="entity">The cardholder entity associated with the change.</param>
    void Entity_CustomFieldsChange(object sender, EventArgs e, Cardholder entity);

    /// <summary>
    /// Determines if a cardholder has another active PTI credential besides the current one.
    /// </summary>
    /// <param name="cardholder">The cardholder to check for additional active credentials.</param>
    /// <param name="currentCredential">The current credential being evaluated.</param>
    /// <returns>A task that returns true if another active PTI credential exists, false otherwise.</returns>
    Task<bool> HasAnotherActivePtiCredentialAsync(Cardholder cardholder, Credential currentCredential);

    /// <summary>
    /// Initializes the credential state handler with custom field definitions.
    /// </summary>
    /// <param name="isUseCustomField">Custom field data for the "IsUsed" field.</param>
    /// <param name="departmentCustomField">Custom field data for the department field.</param>
    /// <returns>A Language.Ext unit value indicating successful initialization.</returns>
    LanguageExt.Unit Initialize(SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO isUseCustomField, SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO departmentCustomField);
}
