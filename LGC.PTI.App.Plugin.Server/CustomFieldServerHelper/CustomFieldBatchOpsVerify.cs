using Genetec.Sdk.Entities.CustomFields;

namespace LGC.PTI.App.Plugin.Server.CustomFieldServerHelper;

/// <summary>
/// Provides batch operations for verifying and creating custom fields.
/// </summary>
public static class CustomFieldBatchOpsVerify
{

    /// <summary>
    /// Iterates through a heterogeneous list (Cardholder, Credential, Door):
    /// - If exists → OK
    /// - If not exists → attempts to create based on EntityType
    /// - Accumulates errors per item and throws an exception with the summary at the end
    /// </summary>
    /// <param name="engine">The engine instance used to manage custom fields.</param>
    /// <param name="fields">The collection of custom field definitions to ensure.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when one or more custom fields could not be created.</exception>
    public static async Task EnsureCustomFieldsAsync(IEngine engine, IEnumerable<CustomFieldDataBO> fields, CancellationToken ct = default)
    {
        var errors = new List<string>();

        foreach (var data in fields)
        {
            var customFielData = await CustomFieldManagerSS
                .TryGetCustomFieldAsync(engine, data.EntityType, data.Name)
                .RunAsync();

            var CustomFieldExist = customFielData.Match(Succ: opt => opt.IsSome, Fail: _ => false);

            if (!CustomFieldExist)
            {
                var fin = await AddByEntityTypeAsync(engine, data, ct);

                if (fin.IsFail)
                {
                    errors.Add($"'{data.Name}' ({data.EntityType}): {((LanguageExt.Common.Error)fin).Message}");
                }
            }
        }

        if (errors.Count > 0)
        {
            var msg = "Could not create some Custom Fields:\n- " + string.Join("\n- ", errors);
            throw new InvalidOperationException(msg);
        }
    }


    /// <summary>
    /// Selects the correct TEntity for TryAddCustomFieldAsync&lt;TEntity&gt; based on EntityType.
    /// Add the cases you use in your solution here.
    /// </summary>
    /// <param name="engine">The engine instance used to add custom fields.</param>
    /// <param name="data">The custom field data to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Fin}"/> containing the created custom field or an error.</returns>
    /// <exception cref="NotSupportedException">Thrown when the EntityType is not supported.</exception>
    private static Task<Fin<CustomField>> AddByEntityTypeAsync(IEngine engine, CustomFieldDataBO data, CancellationToken ct)
    {
        return data.EntityType switch
        {
            EntityType.Cardholder => CustomFieldManagerSS
                .TryAddCustomFieldAsync<Cardholder>(engine, data, ct).RunAsync(),

            EntityType.Door => CustomFieldManagerSS
                .TryAddCustomFieldAsync<Door>(engine, data, ct).RunAsync(),

            EntityType.Credential => CustomFieldManagerSS
                .TryAddCustomFieldAsync<Credential>(engine, data, ct).RunAsync(),

            _ => throw new NotSupportedException($"EntityType not supported: {data.EntityType}")
        };
    }
}
