namespace LGC.PTI.HTTPClient.Refit.Employees;

/// <summary>
/// Wraps PTI Employee API operations with effect-based error handling.
/// Provides higher-level methods for employee management with logging and response validation.
/// </summary>
public interface IEmployeeApiSrv
{
    #region Public Methods

    /// <summary>
    /// Attempts to create a new employee in the PTI system.
    /// </summary>
    /// <param name="employee">The non-null employee data to create.</param>
    /// <returns>An effect wrapping the response or error details.</returns>
    Aff<EmployeesResponse> TryCreateEmployee(NotNull<EmployeeOutDto> employee);

    /// <summary>
    /// Attempts to retrieve a single employee by employee ID.
    /// Throws an exception if the employee is not found.
    /// </summary>
    /// <param name="employeeId">The non-empty employee ID to retrieve.</param>
    /// <returns>An effect wrapping the employee data or an error.</returns>
    Aff<EmployeeInDto> TryGetEmployee(NotEmptyString employeeId);

    /// <summary>
    /// Attempts to retrieve a single employee by employee ID, returning None if not found.
    /// </summary>
    /// <param name="employeeId">The non-empty employee ID to retrieve.</param>
    /// <returns>An effect wrapping an Option of the employee data (None if not found).</returns>
    Aff<Option<EmployeeInDto>> TryGetEmployeeOptional(NotEmptyString employeeId);

    /// <summary>
    /// Attempts to retrieve all employees from the PTI system with pagination support.
    /// </summary>
    /// <returns>An effect wrapping a read-only collection of all employees.</returns>
    Aff<IReadOnlyCollection<EmployeeInDto>> TryGetEmployees();

    /// <summary>
    /// Attempts to update an existing employee in the PTI system.
    /// </summary>
    /// <param name="employee">The non-null employee data to update.</param>
    /// <returns>An effect wrapping the response or error details.</returns>
    Aff<EmployeesResponse> TryUpdateEmployee(NotNull<EmployeeOutDto> employee);

    /// <summary>
    /// Attempts to update an employee's status (active/inactive) in the PTI system.
    /// </summary>
    /// <param name="employee">The non-null employee status data to update.</param>
    /// <returns>An effect wrapping the response or error details.</returns>
    Aff<EmployeesResponse> TryUpdateEmployeeStatus(NotNull<EmployeeStatusOutDto> employee);

    #endregion Public Methods
}