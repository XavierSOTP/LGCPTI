using SoftSolute.Common.Utilities;
using SoftSolute.Sdk.Common.Incidents;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Provides helper methods for creating and posting PTI-related incidents in Genetec.
/// </summary>
public static class IncidentCreator
{
    /// <summary>
    /// Defines the error message template used when an employee operation fails.
    /// </summary>
    private const string ERROR_COMPOSITE = "PTI Error, could not {0} employee: {1} with cardId: {2}";

    /// <summary>
    /// Creates and posts a PTI incident associated with the specified employee.
    /// </summary>
    /// <param name="engine">
    /// The Genetec SDK engine used to create and post the incident.
    /// </param>
    /// <param name="title">
    /// The action or operation that failed and will be included in the incident message.
    /// </param>
    /// <param name="cardholder">
    /// The employee entity associated with the incident.
    /// </param>
    /// <remarks>
    /// The incident is associated with the employee's cardholder identifier and includes
    /// the employee name and credential identifier in the incident message.
    /// </remarks>
    internal static void CreateIncident(IEngine engine, string title, EmployeeEntity cardholder)
    {
        var message = string.Format(CultureInfo.InvariantCulture, ERROR_COMPOSITE, title, cardholder.Name, cardholder.CredentialGuid);
        IncidentManagerSS
            .CreateAndPostIncident(engine, cardholder.CardholderGuid.AsList().AsNotEmptySeq(), nameof(PTI), message);
    }

    /// <summary>
    /// Creates and posts a PTI incident associated with the specified entity identifier.
    /// </summary>
    /// <param name="engine">
    /// The Genetec SDK engine used to create and post the incident.
    /// </param>
    /// <param name="title">
    /// The action or operation that failed and will be included in the incident message.
    /// </param>
    /// <param name="reference">
    /// The unique identifier of the entity associated with the incident.
    /// </param>
    internal static void CreateIncident(IEngine engine, string title, Guid reference)
    {
        IncidentManagerSS
            .CreateAndPostIncident(engine, reference.AsList().AsNotEmptySeq(), nameof(PTI), $"Error {title} Entity with guid: {reference}");
    }
}