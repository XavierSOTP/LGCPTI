namespace LGC.PTI.Refit.Authentication;

/// <summary>
/// Provides HTTP API interface for communicating with the PTI Employee REST API using Refit.
/// Supports CRUD operations for employee records including creation, retrieval, updates, and status management.
/// </summary>
public interface IPtiEmployeeApi
{
    #region Public Methods

    /// <summary>
    /// Creates a new employee in the PTI system.
    /// </summary>
    /// <param name="body">The employee data to create.</param>
    /// <returns>A task that returns the response from the server.</returns>
    [Post("/api/Employee")]
    Task<EmployeesResponse> CreateEmployee([Body(BodySerializationMethod.Serialized)] EmployeeOutDto body);

    /// <summary>
    /// Retrieves a paginated list of employees from the PTI system.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of employees per page.</param>
    /// <returns>A task that returns a collection of employees in the response.</returns>
    [Get("/api/Employee")]
    Task<EmployeesResponse> GetEmployees(
        [AliasAs("page")] int pageNumber,
        [AliasAs("pageSize")] int pageSize);

    /// <summary>
    /// Updates an existing employee in the PTI system.
    /// </summary>
    /// <typeparam name="T">The type of employee data being sent (must derive from EmployeeBaseDto).</typeparam>
    /// <typeparam name="TResponse">The type of response returned (must derive from EmployeesResponse).</typeparam>
    /// <param name="employeeNumber">The unique employee number identifier.</param>
    /// <param name="body">The updated employee data.</param>
    /// <returns>A task that returns the update response from the server.</returns>
    [Put("/api/Employee/{employeeNumber}")]
    Task<TResponse> UpdateEmployee<T, TResponse>(string employeeNumber, [Body(BodySerializationMethod.Serialized)] T body)
        where T : EmployeeBaseDto
        where TResponse : EmployeesResponse;

    /// <summary>
    /// Updates an employee's PIN/password in the PTI system.
    /// </summary>
    /// <param name="body">The employee PIN update data.</param>
    /// <returns>A task that returns the update response from the server.</returns>
    [Post("/api/Employee/UpdateEmployeeSetPin")]
    Task<EmployeesResponse> UpdateEmployeeSetPin([Body(BodySerializationMethod.Serialized)] EmployeePinOutDto body);

    /// <summary>
    /// Updates an employee's status (active/inactive) in the PTI system.
    /// </summary>
    /// <param name="body">The employee status update data.</param>
    /// <returns>A task that returns the update response from the server.</returns>
    [Put("/api/Employee/UpdateEmployeeStatus")]
    Task<EmployeesResponse> UpdateEmployeeStatus([Body(BodySerializationMethod.Serialized)] EmployeeStatusOutDto body);

    #endregion Public Methods
}