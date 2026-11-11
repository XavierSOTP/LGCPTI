namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Provides helper methods for filtering entity update information used by observers.
/// </summary>
internal static class ObserverHelper
{
    /// <summary>
    /// Returns the identifiers of entities that match the specified entity type.
    /// </summary>
    /// <param name="updateInfos">
    /// The entity update records to evaluate.
    /// </param>
    /// <param name="type">
    /// The entity type used to filter the update records.
    /// </param>
    /// <returns>
    /// A sequence containing the identifiers of entities whose type matches
    /// <paramref name="type"/>.
    /// </returns>
    internal static IEnumerable<Guid> ValidEntityType(
    IEnumerable<EntityUpdateInfo> updateInfos, EntityTypeSS type)
    {
        return updateInfos
            .Where(ent => type.AreEqual(ent.EntityType))
            .Select(ent => ent.EntityGuid);
    }
}