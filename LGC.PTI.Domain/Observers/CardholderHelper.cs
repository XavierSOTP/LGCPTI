using Microsoft.Extensions.Logging;
using SoftSolute.Common.Utilities;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Helper class for validating and creating employee entities from cardholder data.
/// Provides utilities for checking cardholder validity and converting cardholders to employee records.
/// </summary>
internal static class CardholderHelper
{
    /// <summary>
    /// Determines if a cardholder is valid based on membership in the specified group and active status.
    /// </summary>
    /// <param name="cardholder">The cardholder to validate.</param>
    /// <param name="cardholderGroup">The cardholder group that the cardholder must belong to.</param>
    /// <returns>True if the cardholder is active and belongs to the specified group; otherwise false.</returns>
    internal static bool IsValidCardholder(Cardholder cardholder, Guid cardholderGroup)
    {
        if (cardholder.IsNullOrDefault() || cardholderGroup.IsNullOrDefault()) return false;

        var result = Optional(cardholder)
            .KeepAsSomeIf(ch => CardholderStateSS.Active.AreEqual(cardholder.Status.State))
            .KeepAsSomeIf(ch => ch.Parents.Contains(cardholderGroup));
        return result.IsSome;
    }

    /// <summary>
    /// Creates an inactive employee entity from a cardholder for deactivation purposes.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="cardholder">The cardholder to convert to an inactive employee entity.</param>
    /// <returns>An Option containing the employee entity if creation succeeds, or None if it fails.</returns>
    internal static Option<EmployeeEntity> NewInactive(Microsoft.Extensions.Logging.ILogger logger, NotNull<Cardholder> cardholder)
    {
        logger.LogInformation("Building Employee for deactivation. CardholderGuid: {CardholderGuid}, Name: {Name}, FirstName: {FirstName}, LastName: {LastName}", cardholder.Value.Guid, cardholder.Value.Name, cardholder.Value.FirstName, cardholder.Value.LastName);

        var builder = EmployeeBuilder.NewForDeactivation()
            .SetState(false)
            .New(cardholder);

        var result = builder.TryBuild();

        result.IfFail(ex => logger.LogError(ex, "Error building EmployeeEntity for deactivation. CardholderGuid: {CardholderGuid}", cardholder.Value.Guid));

        return result.Match(
            Succ: employee => Prelude.Some(employee),
            Fail: _ => Option<EmployeeEntity>.None);
    }

    /// <summary>
    /// Creates an active employee entity from a credential and related department information.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="credential">The credential to use for building the active employee.</param>
    /// <param name="department">The department to assign to the employee.</param>
    /// <returns>An Option containing the employee entity if creation succeeds, or None if it fails.</returns>
    internal static Option<EmployeeEntity> NewActive(
     Microsoft.Extensions.Logging.ILogger logger,
     NotNull<Credential> credential,
     NotEmptyString department)
    {
        var builder = EmployeeBuilder.New()
            .SetDepartment(department)
            .SetState(true)
            .SetBadge(credential)
            .New(credential.Value.Cardholder);

        logger.LogInformation(
    "Building employee from Credential Cardholder. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}, Name: {Name}, FirstName: {FirstName}, LastName: {LastName}",
    credential.Value.Guid,
    credential.Value.Cardholder.Guid,
    credential.Value.Cardholder.Name,
    credential.Value.Cardholder.FirstName,
    credential.Value.Cardholder.LastName);
        var result = builder.TryBuild();

        result.IfFail(ex => logger.LogError(ex, "Error building EmployeeEntity"));

        return result.Match(
            Succ: employee => Prelude.Some(employee),
            Fail: _ => Option<EmployeeEntity>.None);
    }


}