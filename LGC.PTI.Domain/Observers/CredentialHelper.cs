namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Helper class for credential-related operations and queries.
/// Provides utilities for checking credential state and custom field values.
/// </summary>
internal static class CredentialHelper
{
    /// <summary>
    /// Determines if a credential is in an active state.
    /// </summary>
    /// <param name="cr">The credential to check.</param>
    /// <returns>True if the credential is active; otherwise false.</returns>
    internal static bool IsCredActive(Credential cr)
    {
        return CredentialStateSS.Active.AreEqual(cr.Status.State);
    }

    /// <summary>
    /// Asynchronously checks if a credential has the PTI usage custom field set to true.
    /// </summary>
    /// <param name="credential">The credential to check.</param>
    /// <param name="customField">The custom field definition to check for PTI usage.</param>
    /// <returns>A task that returns true if the credential is marked for LGC/PTI usage, false otherwise.</returns>
    internal static async Task<bool> IsUsedLGC(
    Credential credential,
    SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO customField)
    {
        var customFields = await credential
            .GetCustomFieldsAsync()
            .ConfigureAwait(false);

        var customFieldValue = customFields
            .FirstOrDefault(x =>
                x.CustomField.Guid == customField.CustomFieldId);

        if (customFieldValue is null)
        {
            return false;
        }

        return customFieldValue.Value is bool value && value;
    }
}