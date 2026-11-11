using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.HTTPClient.Refit.Employees;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Refit;
using SoftSolute.Common.Utilities;
using SoftSolute.Utiities.Plugin.Adapters;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Manages synchronization of employee entities with the external PTI API.
/// </summary>
/// <remarks>
/// The manager creates, updates, retrieves, and deactivates PTI employee records
/// based on employee information derived from Genetec cardholders and credentials.
/// </remarks>
public sealed class EmployeeManager : IEmployeeManager
{
    /// <summary>
    /// Gets the Genetec SDK engine used for entity access and incident creation.
    /// </summary>
    private IEngine Engine { get; }

    /// <summary>
    /// Gets the logger used for diagnostic and operational messages.
    /// </summary>
    private ILogger<EmployeeManager> Logger { get; }

    /// <summary>
    /// Gets the service used to communicate with the external PTI employee API.
    /// </summary>
    private IEmployeeApiSrv EmployeeApiSrv { get; }

    /// <summary>
    /// Gets the mapper used to convert domain employee entities to PTI API DTOs.
    /// </summary>
    private IMapper Mapper { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeManager"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used for diagnostic and operational messages.
    /// </param>
    /// <param name="engine">
    /// The Genetec SDK engine used for entity-related operations.
    /// </param>
    /// <param name="employeeApiSrv">
    /// The service used to communicate with the PTI employee API.
    /// </param>
    /// <param name="mapper">
    /// The mapper used to convert employee entities to PTI API DTOs.
    /// </param>
    public EmployeeManager(ILogger<EmployeeManager> logger, IEngine engine, IEmployeeApiSrv employeeApiSrv, IMapper mapper)
    {
        Engine = engine;
        Logger = logger;
        EmployeeApiSrv = employeeApiSrv;
        Mapper = mapper;
    }

    /// <summary>
    /// Performs the initial synchronization of a collection of active employees with PTI.
    /// </summary>
    /// <param name="employees">
    /// The employees to create or update in PTI.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous synchronization operation.
    /// </returns>
    /// <remarks>
    /// Existing PTI employees are retrieved once and indexed by employee number.
    /// Each supplied employee is then either created or updated, with processing performed
    /// concurrently using a maximum concurrency of 10.
    /// </remarks>
    public async Task AddOrUpdateEmployeesFirstAsync(IReadOnlyCollection<EmployeeEntity> employees)
    {
        Logger.LogInformation("Initial employee synchronization started. EmployeesToProcess: {EmployeeCount}", employees.Count);

        Logger.LogInformation("Retrieving existing employees from PTI.");

        var result = await EmployeeApiSrv
            .TryGetEmployees()
            .RunAsync();

        var ptiEmployees = result.ForceGetValue();

        Logger.LogInformation("Existing employees retrieved from PTI. EmployeeCount: {EmployeeCount}", ptiEmployees.Count);

        var existingEmployees = ptiEmployees
            .Where(employee => !string.IsNullOrWhiteSpace(employee.EmployeeNumber))
            .GroupBy(employee => employee.EmployeeNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        Logger.LogInformation("PTI employee lookup created. ValidEmployeeCount: {EmployeeCount}", existingEmployees.Count);

        await employees
            .ForEachAsync(employee => AddOrUpdateEmployeeAsyncImpl(employee, existingEmployees), concurrency: 10)
            .IgnoreCtx();

        Logger.LogInformation("Initial employee synchronization completed. EmployeesProcessed: {EmployeeCount}", employees.Count);
    }

    /// <summary>
    /// Creates or updates a single employee during the initial batch synchronization.
    /// </summary>
    /// <param name="entity">
    /// The active employee entity to synchronize with PTI.
    /// </param>
    /// <param name="existingEmployees">
    /// A lookup of existing PTI employees keyed by employee number.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous create or update operation.
    /// </returns>
    /// <remarks>
    /// Employees already present in <paramref name="existingEmployees"/> are updated;
    /// otherwise, a new PTI employee is created. API failures are logged and generate
    /// a Genetec incident.
    /// </remarks>
    private async Task AddOrUpdateEmployeeAsyncImpl(EmployeeEntity entity, IReadOnlyDictionary<string, EmployeeInDto> existingEmployees)
    {
        Debug.Assert(entity.Active, $"{nameof(EmployeeOutDto)} should be active at this stage.");

        Logger.LogInformation("Processing Employee during initial synchronization. EmployeeNumber: {EmployeeNumber}, CardholderGuid: {CardholderGuid}", entity.EmployeeNumber, entity.CardholderGuid);

        var empOutDto = Mapper.Map<EmployeeOutDto>(entity);

        if (existingEmployees.ContainsKey(entity.EmployeeNumber))
        {
            var action = CRUDAction.Updated;

            Logger.LogInformation("Employee already exists in PTI. Updating Employee. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);

            _ = await EmployeeApiSrv
                .TryUpdateEmployee(empOutDto)
                .OnSucc(_ =>
                {
                    Logger.LogInformation("Employee successfully updated in PTI. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);
                    Logger.LogEmployeeAction(entity, action);
                })
                .OnFail(ex =>
                {
                    Logger.LogError("Failed to update Employee in PTI. EmployeeNumber: {EmployeeNumber}, Error: {Error}", entity.EmployeeNumber, GetError(ex));
                    Logger.LogErrorOnEmployeeAction(entity, action);
                    IncidentCreator.CreateIncident(Engine, title: nameof(action), entity);
                })
                .RunAsync()
                .IgnoreCtx();

            return;
        }

        {
            var action = CRUDAction.Added;

            Logger.LogInformation("Employee does not exist in PTI. Creating Employee. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);

            _ = await EmployeeApiSrv
                .TryCreateEmployee(empOutDto)
                .OnSucc(_ =>
                {
                    Logger.LogInformation("Employee successfully created in PTI. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);
                    Logger.LogEmployeeAction(entity, action);
                })
                .OnFail(ex =>
                {
                    Logger.LogError("Failed to create Employee in PTI. EmployeeNumber: {EmployeeNumber}, Error: {Error}", entity.EmployeeNumber, GetError(ex));
                    Logger.LogErrorOnEmployeeAction(entity, action);
                    IncidentCreator.CreateIncident(Engine, title: nameof(action), entity);
                })
                .RunAsync()
                .IgnoreCtx();
        }

        static string GetError(Exception ex)
        {
            return ex switch
            {
                ApiException ex1 => ex1.Content,
                _ => ex.Message
            };
        }
    }

    /// <summary>
    /// Creates or updates a single active employee in PTI.
    /// </summary>
    /// <param name="entity">
    /// The employee entity to synchronize.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous create or update operation.
    /// </returns>
    /// <remarks>
    /// The employee is first retrieved from PTI by employee number. If an existing
    /// employee is found, it is updated; otherwise, a new employee is created.
    /// API failures are logged and generate a Genetec incident.
    /// </remarks>
    public async Task AddOrUpdateEmployeeAsync(EmployeeEntity entity)
    {
        Debug.Assert(entity.Active, $"{nameof(EmployeeOutDto)} should be active at this stage.");

        Logger.LogInformation("Processing Employee add or update. EmployeeNumber: {EmployeeNumber}, CardholderGuid: {CardholderGuid}", entity.EmployeeNumber, entity.CardholderGuid);

        Logger.LogInformation("Searching Employee in PTI. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);

        var result = await EmployeeApiSrv
            .TryGetEmployee(entity.EmployeeNumber)
            .RunAsync();

        var existingEmployee = result.ForceGetValue();

        var empOutDto = Mapper.Map<EmployeeOutDto>(entity);

        if (existingEmployee is not null && !string.IsNullOrWhiteSpace(existingEmployee.EmployeeNumber))
        {
            var action = CRUDAction.Updated;

            Logger.LogInformation("Employee found in PTI. Updating Employee. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);

            _ = await EmployeeApiSrv
                .TryUpdateEmployee(empOutDto)
                .OnSucc(_ =>
                {
                    Logger.LogInformation("Employee successfully updated in PTI. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);
                    Logger.LogEmployeeAction(entity, action);
                })
                .OnFail(ex =>
                {
                    Logger.LogError("Failed to update Employee in PTI. EmployeeNumber: {EmployeeNumber}, Error: {Error}", entity.EmployeeNumber, GetError(ex));
                    Logger.LogErrorOnEmployeeAction(entity, action);
                    IncidentCreator.CreateIncident(Engine, title: nameof(action), entity);
                })
                .RunAsync()
                .IgnoreCtx();
        }
        else
        {
            var action = CRUDAction.Added;

            Logger.LogInformation("Employee not found in PTI. Creating Employee. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);

            _ = await EmployeeApiSrv
                .TryCreateEmployee(empOutDto)
                .OnSucc(_ =>
                {
                    Logger.LogInformation("Employee successfully created in PTI. EmployeeNumber: {EmployeeNumber}", entity.EmployeeNumber);
                    Logger.LogEmployeeAction(entity, action);
                })
                .OnFail(ex =>
                {
                    Logger.LogError("Failed to create Employee in PTI. EmployeeNumber: {EmployeeNumber}, Error: {Error}", entity.EmployeeNumber, GetError(ex));
                    Logger.LogErrorOnEmployeeAction(entity, action);
                    IncidentCreator.CreateIncident(Engine, title: nameof(action), entity);
                })
                .RunAsync()
                .IgnoreCtx();
        }

        static string GetError(Exception ex)
        {
            return ex switch
            {
                ApiException ex1 => ex1.Content,
                _ => ex.Message
            };
        }
    }

    /// <summary>
    /// Attempts to force the specified employee into a deactivated state in PTI.
    /// </summary>
    /// <param name="empEntity">
    /// The employee entity to deactivate.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous deactivation operation.
    /// </returns>
    /// <remarks>
    /// The employee is first retrieved from PTI. Deactivation is skipped when the employee
    /// cannot be retrieved or does not exist. If found, the employee status is updated to deactivated.
    /// </remarks>
    public async Task TryForceDeactivateEmployeeAsync(EmployeeEntity empEntity)
    {
        if (empEntity is null)
        {
            Logger.LogWarning("Force Employee deactivation skipped because EmployeeEntity is null.");
            return;
        }

        Logger.LogInformation("Force Employee deactivation requested. EmployeeNumber: {EmployeeNumber}", empEntity.EmployeeNumber);

        var result = await EmployeeApiSrv.TryGetEmployee(empEntity.EmployeeNumber).RunAsync().IgnoreCtx();

        if (result.IsFail)
        {
            result.IfFail(error => Logger.LogError("Could not retrieve Employee before deactivation. EmployeeNumber: {EmployeeNumber}, Error: {Error}", empEntity.EmployeeNumber, error.Message));
            return;
        }

        var employee = result.ForceGetValue();

        if (string.IsNullOrWhiteSpace(employee.EmployeeNumber))
        {
            Logger.LogWarning("Employee was not found in PTI. Deactivation skipped. EmployeeNumber: {EmployeeNumber}", empEntity.EmployeeNumber);
            return;
        }

        var deactivated = await TryDeactivateEmployeeBool(empEntity).IgnoreCtx();

        if (!deactivated)
        {
            Logger.LogError("Force Employee deactivation failed. EmployeeNumber: {EmployeeNumber}", empEntity.EmployeeNumber);
            return;
        }

        Logger.LogInformation("Force Employee deactivation completed. EmployeeNumber: {EmployeeNumber}", empEntity.EmployeeNumber);
    }

    /// <summary>
    /// Updates the specified employee's PTI status to deactivated.
    /// </summary>
    /// <param name="employeeModel">
    /// The employee entity whose PTI status will be updated.
    /// </param>
    /// <returns>
    /// A task that returns <see langword="true"/> when the PTI status update succeeds;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Failed deactivation attempts are logged, recorded as employee action errors,
    /// and generate a Genetec incident.
    /// </remarks>
    private async Task<bool> TryDeactivateEmployeeBool(EmployeeEntity employeeModel)
    {
        Logger.LogInformation("Employee deactivation started. EmployeeNumber: {EmployeeNumber}", employeeModel.EmployeeNumber);

        var emp = Mapper.Map<EmployeeEntity, EmployeeOutDto>(employeeModel);
        var empStatusDto = EmployeeStatusOutDto.Deactivated(emp.EmployeeNumber);
        var updateRlst = await EmployeeApiSrv.TryUpdateEmployeeStatus(empStatusDto).RunAsync().IgnoreCtx();

        if (updateRlst.IsFail)
        {
            updateRlst.IfFail(error => Logger.LogError("Failed to deactivate Employee in PTI. EmployeeNumber: {EmployeeNumber}, Error: {Error}", employeeModel.EmployeeNumber, error.Message));

            Logger.LogErrorOnEmployeeAction(employeeModel, CRUDAction.Deleted);
            IncidentCreator.CreateIncident(Engine, title: nameof(CRUDAction.Deleted), employeeModel);

            return false;
        }

        Logger.LogInformation("Employee successfully deactivated in PTI. EmployeeNumber: {EmployeeNumber}", employeeModel.EmployeeNumber);
        Logger.LogEmployeeAction(employeeModel, CRUDAction.Deleted);

        return true;
    }

    /// <summary>
    /// Determines whether an employee model exists for the specified cardholder.
    /// </summary>
    /// <param name="cardholderGuid">
    /// The non-empty cardholder identifier to evaluate.
    /// </param>
    /// <returns>
    /// An asynchronous operation intended to return whether an employee model exists.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this operation is not currently implemented.
    /// </exception>
    public TryAsync<bool> EmployeeModelExistsAsync(NotEmptyGuid cardholderGuid)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves all employee models.
    /// </summary>
    /// <returns>
    /// A task intended to return all available employee models.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this operation is not currently implemented.
    /// </exception>
    public Task<IReadOnlyCollection<EmployeeModel>> GetAllEmployeeModelAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Attempts to deactivate the employee associated with the specified cardholder identifier.
    /// </summary>
    /// <param name="cardholderId">
    /// The cardholder identifier associated with the employee.
    /// </param>
    /// <returns>
    /// A task intended to represent the asynchronous deactivation operation.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this operation is not currently implemented.
    /// </exception>
    public Task TryDeactivateEmployeeAsync(Guid cardholderId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Attempts to deactivate the employee associated with the specified credential identifier.
    /// </summary>
    /// <param name="credGuid">
    /// The credential identifier associated with the employee.
    /// </param>
    /// <returns>
    /// A task intended to represent the asynchronous deactivation operation.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this operation is not currently implemented.
    /// </exception>
    public Task TryDeactivateEmployeeByCredAsync(Guid credGuid)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Attempts to find the employee associated with the specified cardholder identifier.
    /// </summary>
    /// <param name="cardholderId">
    /// The cardholder identifier used to locate the employee.
    /// </param>
    /// <returns>
    /// An asynchronous optional operation intended to return the matching employee model.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this operation is not currently implemented.
    /// </exception>
    public TryOptionAsync<EmployeeModel> TryFindEmployeeAsync(Guid cardholderId)
    {
        throw new NotImplementedException();
    }
}