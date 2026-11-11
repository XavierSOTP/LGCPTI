using SoftSolute.Common.Utilities;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Manages employee entities in the PTI system, handling creation, updates, deactivation,
/// and synchronization with external employee data sources.
/// </summary>
public interface IEmployeeManager
{
    /// <summary>
    /// Adds or updates a single employee entity.
    /// </summary>
    /// <param name="entity">The employee entity to add or update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddOrUpdateEmployeeAsync(EmployeeEntity entity);

    /// <summary>
    /// Performs initial batch add or update of employee entities.
    /// Synchronizes a collection of new employees with existing employees in the PTI system.
    /// </summary>
    /// <param name="employees">The collection of employee entities to add or update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddOrUpdateEmployeesFirstAsync(IReadOnlyCollection<EmployeeEntity> employees);

    /// <summary>
    /// Checks if an employee model exists for the specified cardholder.
    /// </summary>
    /// <param name="cardholderGuid">The unique identifier of the cardholder.</param>
    /// <returns>A try-async that returns true if the employee exists, false otherwise.</returns>
    TryAsync<bool> EmployeeModelExistsAsync(NotEmptyGuid cardholderGuid);

    /// <summary>
    /// Retrieves all employee models from the system.
    /// </summary>
    /// <returns>A task that returns a collection of all employee models.</returns>
    Task<IReadOnlyCollection<EmployeeModel>> GetAllEmployeeModelAsync();

    /// <summary>
    /// Attempts to deactivate an employee based on their cardholder identifier.
    /// </summary>
    /// <param name="cardholderId">The unique identifier of the cardholder.</param>
    /// <returns>A task representing the asynchronous deactivation operation.</returns>
    Task TryDeactivateEmployeeAsync(Guid cardholderId);

    /// <summary>
    /// Attempts to deactivate an employee based on their credential identifier.
    /// </summary>
    /// <param name="credGuid">The unique identifier of the credential.</param>
    /// <returns>A task representing the asynchronous deactivation operation.</returns>
    Task TryDeactivateEmployeeByCredAsync(Guid credGuid);

    /// <summary>
    /// Attempts to find an employee model by cardholder identifier.
    /// </summary>
    /// <param name="cardholderId">The unique identifier of the cardholder.</param>
    /// <returns>A try-option-async that returns the employee model if found, or None if not found.</returns>
    TryOptionAsync<EmployeeModel> TryFindEmployeeAsync(Guid cardholderId);

    /// <summary>
    /// Executes forced deactivation of an employee when a cardholder is deleted from the system.
    /// </summary>
    /// <param name="cardholderId">The employee entity representing the cardholder being deleted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TryForceDeactivateEmployeeAsync(EmployeeEntity cardholderId);
}