using Microsoft.Extensions.Logging;
using SoftSolute.Common.Utilities.CollectionSS;
using SoftSolute.Utiities.Plugin.Adapters;

namespace LGC.PTI.HTTPClient.Refit.Employees;

/// <summary>
/// Provides employee operations against the PTI Employee API.
/// </summary>
/// <remarks>
/// The service wraps the underlying Refit client with LanguageExt effects and handles
/// employee creation, retrieval, pagination, updates, status changes, logging, and API response validation.
/// </remarks>
public sealed class EmployeeApiSrv : IEmployeeApiSrv
{
    #region Private Fields

    /// <summary>
    /// Defines the default page size used when retrieving employees from PTI.
    /// </summary>
    private const int PAGESIZE = 50;

    #endregion Private Fields

    #region Private Properties

    /// <summary>
    /// Gets the underlying PTI Employee API client.
    /// </summary>
    private IPtiEmployeeApi ClientApi { get; }

    /// <summary>
    /// Gets the logger used for diagnostic and operational messages.
    /// </summary>
    private Microsoft.Extensions.Logging.ILogger<EmployeeApiSrv> Logger { get; }

    #endregion Private Properties

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeApiSrv"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used for diagnostic and operational messages.
    /// </param>
    /// <param name="client">
    /// The PTI Employee API client used to execute HTTP operations.
    /// </param>
    public EmployeeApiSrv(Microsoft.Extensions.Logging.ILogger<EmployeeApiSrv> logger, IPtiEmployeeApi client)
    {
        ClientApi = client;
        Logger = logger;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Attempts to create a new employee in PTI.
    /// </summary>
    /// <param name="employee">
    /// The employee data to create.
    /// </param>
    /// <returns>
    /// An asynchronous effect that returns the PTI employee response when the operation succeeds.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the PTI response is null, invalid, or indicates that employee creation failed.
    /// </exception>
    public Aff<EmployeesResponse> TryCreateEmployee(NotNull<EmployeeOutDto> employee)
    {
        return LanguageExtAffExtensions.Aff<EmployeesResponse>(async () =>
        {
            Logger.LogInformation(employee.Value.ToString());
            var employeesResponse = await ClientApi.CreateEmployee(employee).IgnoreCtx();
            if (employeesResponse.IsNullOrDefault() || !employeesResponse.IsSuccess)
            {
                Logger.LogError(employeesResponse.Errors.ToString());
                throw new InvalidOperationException($"Could not create employee: {employeesResponse.Errors.ConvertToString()}");
            }
            return employeesResponse;
        });
    }

    /// <summary>
    /// Attempts to find an employee by employee number.
    /// </summary>
    /// <param name="employeeId">
    /// The employee number to search for.
    /// </param>
    /// <returns>
    /// An asynchronous effect containing the matching employee when found;
    /// otherwise, <see cref="Option{T}.None"/>.
    /// </returns>
    /// <remarks>
    /// Employees are retrieved page by page until a case-insensitive employee-number match
    /// is found or the final page is reached.
    /// </remarks>
    public Aff<Option<EmployeeInDto>> TryGetEmployeeOptional(NotEmptyString employeeId)
    {
        return LanguageExtAffExtensions.Aff<Option<EmployeeInDto>>(async () =>
        {
            foreach (var page in IEnumerableHelper.InfiniteIntCounter(1))
            {
                var pageResults = await TryGetEmployees(page, PAGESIZE).IgnoreCtx();

                var found = pageResults.results.Find(empl =>
                    string.Equals(
                        empl.EmployeeNumber,
                        employeeId,
                        StringComparison.OrdinalIgnoreCase));

                if (found.IsSome)
                {
                    return found;
                }

                if (!pageResults.hasNextPage)
                {
                    break;
                }
            }

            return Option<EmployeeInDto>.None;
        });
    }

    /// <summary>
    /// Attempts to retrieve an employee by employee number.
    /// </summary>
    /// <param name="employeeId">
    /// The employee number to search for.
    /// </param>
    /// <returns>
    /// An asynchronous effect containing the matching employee when found.
    /// If no employee is found, a new empty <see cref="EmployeeInDto"/> instance is returned.
    /// </returns>
    /// <remarks>
    /// Employees are retrieved page by page until a case-insensitive employee-number match
    /// is found or the final page is reached.
    /// </remarks>
    public Aff<EmployeeInDto> TryGetEmployee(NotEmptyString employeeId)
    {
        return LanguageExtAffExtensions.Aff<EmployeeInDto>(async () =>
        {
            var empFoundRlst = new EmployeeInDto();

            foreach (var page in IEnumerableHelper.InfiniteIntCounter(1))
            {
                var pageResults = await TryGetEmployees(page, PAGESIZE).IgnoreCtx();

                var found = pageResults.results.Find(empl =>
                    string.Equals(
                        empl.EmployeeNumber,
                        employeeId,
                        StringComparison.OrdinalIgnoreCase));

                _ = found.IfSome(employee => empFoundRlst = employee);

                if (found.IsSome)
                {
                    break;
                }

                if (!pageResults.hasNextPage)
                {
                    break;
                }
            }

            return empFoundRlst;
        });
    }

    /// <summary>
    /// Retrieves all employees from PTI.
    /// </summary>
    /// <returns>
    /// An asynchronous effect containing all employees returned by the paginated PTI API.
    /// </returns>
    /// <remarks>
    /// The method requests pages sequentially until the API indicates that no additional page exists.
    /// </remarks>
    public Aff<IReadOnlyCollection<EmployeeInDto>> TryGetEmployees()
    {
        return LanguageExtAffExtensions.Aff<IReadOnlyCollection<EmployeeInDto>>(async () =>
        {

            var allemployees = new List<EmployeeInDto>();

            foreach (var page in IEnumerableHelper.InfiniteIntCounter(1))
            {
                var pageResults = await TryGetEmployees(page, PAGESIZE).IgnoreCtx();
                allemployees.AddRange(pageResults.results);
                if (!pageResults.hasNextPage) break;


            }
            return allemployees.ToReadOnlyCollection();
        });
    }

    /// <summary>
    /// Retrieves a single page of employees from PTI.
    /// </summary>
    /// <param name="page">
    /// The page number to retrieve.
    /// </param>
    /// <param name="pageSize">
    /// The maximum number of employees to request for the page.
    /// </param>
    /// <returns>
    /// A task containing the employees returned for the requested page and a value indicating
    /// whether another page is available.
    /// </returns>
    /// <remarks>
    /// When the PTI response is invalid or indicates failure, the error is logged and an empty
    /// collection with <see langword="false"/> for the next-page indicator is returned.
    /// </remarks>
    private async Task<(IReadOnlyCollection<EmployeeInDto> results, bool hasNextPage)> TryGetEmployees(int page, int pageSize)
    {
        var employeesresponse = await ClientApi.GetEmployees(page, pageSize).IgnoreCtx();

        if (employeesresponse.IsNullOrDefault() || !employeesresponse.IsSuccess)
        {
            Logger.LogError(employeesresponse.Errors.ToString());
            return (Enumerable.Empty<EmployeeInDto>().ToList(), false);
        }

        var hasnextpage = employeesresponse.Result.MetaData.HasNextPage;
        return (employeesresponse.Result.Items.ToReadOnlyCollection(), hasnextpage);
    }

    /// <summary>
    /// Attempts to update an existing employee in PTI.
    /// </summary>
    /// <param name="employee">
    /// The employee data to update.
    /// </param>
    /// <returns>
    /// An asynchronous effect containing the PTI employee response when the update succeeds.
    /// </returns>
    public Aff<EmployeesResponse> TryUpdateEmployee(NotNull<EmployeeOutDto> employee)
    {
        return TryUpdateEmployeeImpl(() =>
                ClientApi.UpdateEmployee<EmployeeOutDto, EmployeesResponse>(employee.Value.EmployeeNumber, employee));
    }

    /// <summary>
    /// Attempts to update the status of an employee in PTI.
    /// </summary>
    /// <param name="employee">
    /// The employee status data to send to PTI.
    /// </param>
    /// <returns>
    /// An asynchronous effect containing the PTI employee response when the status update succeeds.
    /// </returns>
    public Aff<EmployeesResponse> TryUpdateEmployeeStatus(NotNull<EmployeeStatusOutDto> employee)
    {
        return TryUpdateEmployeeImpl(() => ClientApi.UpdateEmployeeStatus(employee));
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Executes an employee update operation and validates the PTI response.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The PTI response type, which must derive from <see cref="EmployeesResponse"/>.
    /// </typeparam>
    /// <param name="updateFunc">
    /// The asynchronous update operation to execute.
    /// </param>
    /// <returns>
    /// An asynchronous effect containing the successful PTI response.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the PTI response is null, invalid, or indicates that the update failed.
    /// </exception>
    private Aff<TResponse> TryUpdateEmployeeImpl<TResponse>(Func<Task<TResponse>> updateFunc)
   where TResponse : EmployeesResponse
    {
        return LanguageExtAffExtensions.Aff(async () =>
        {
            var employeesResponse = await updateFunc().IgnoreCtx();
            if (employeesResponse.IsNullOrDefault() || !employeesResponse.IsSuccess)
            {
                Logger.LogError(employeesResponse.Errors.ToString());
                throw new InvalidOperationException($"Could not update employee: {employeesResponse.Errors.ConvertToString()}");
            }

            return employeesResponse;
        });
    }

    #endregion Private Methods
}