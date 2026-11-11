using Microsoft.Extensions.Logging;
using Generic = System.Collections.Generic;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Contains cardholder synchronization, credential-processing, and employee-mapping
/// operations used by <see cref="CardholderObserver"/>.
/// </summary>
public sealed partial class CardholderObserver
{
    #region Internal Methods

    /// <summary>
    /// Determines whether the specified cardholder is inactive and, when required,
    /// deactivates the corresponding employee in PTI.
    /// </summary>
    /// <param name="cardholder">
    /// The Genetec cardholder whose current state will be evaluated.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the cardholder is inactive and employee deactivation
    /// is required; otherwise, <see langword="false"/>.
    /// </returns>
    internal async Task<bool> TryDeactivateByCardholderStateAsync(Cardholder cardholder)
    {
        Logger.LogEnteringMethod();

        var isActiveCardholder = cardholder.SomeIf(ch => CardholderStateSS.Active.AreEqual(ch.Status.State));

        if (isActiveCardholder.IsNone)
        {
            Logger.LogInformation("Cardholder is inactive. Employee deactivation required. CardholderGuid: {CardholderGuid}, Name: {Name}", cardholder.Guid, cardholder.Name);

            var employee = CardholderHelper.NewInactive(Logger, cardholder);

            if (employee.IsNone)
            {
                Logger.LogWarning("Employee could not be built for Cardholder deactivation. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                Logger.LogLeavingMethod();
                return true;
            }

            var employeeEntity = employee.ForceGetValue();

            Logger.LogInformation("Deactivating Employee because Cardholder is inactive. CardholderGuid: {CardholderGuid}, EmployeeNumber: {EmployeeNumber}", cardholder.Guid, employeeEntity.EmployeeNumber);

            await EmployeeManager.TryForceDeactivateEmployeeAsync(employeeEntity).IgnoreCtx();

            Logger.LogInformation("Employee deactivation completed for inactive Cardholder. CardholderGuid: {CardholderGuid}, EmployeeNumber: {EmployeeNumber}", cardholder.Guid, employeeEntity.EmployeeNumber);
        }

        Logger.LogLeavingMethod();

        return isActiveCardholder.IsNone;
    }


    /// <summary>
    /// Synchronizes cardholders from the configured Genetec cardholder group with PTI employees.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous synchronization operation.
    /// </returns>
    /// <remarks>
    /// The synchronization loads all cardholders from the configured group, determines which
    /// cardholders have active credentials enabled for PTI, adds or updates the corresponding
    /// employees, and deactivates employees associated with cardholders that are no longer eligible.
    /// </remarks>
    internal async Task SyncRepository()
    {

        var cardholderGroup = Engine.GetEntity<CardholderGroup>(
            Settings.LGCSettings.CardholderGroupId);

        if (cardholderGroup is null)
        {
            Logger.LogWarning("Cardholder group was not found");
            return;
        }

        var allCardholders = cardholderGroup.Children
    .Select(guid => Engine.GetEntity(guid))
    .Where(entity => entity is Cardholder)
    .Cast<Cardholder>()
    .ToList();

        foreach (var item in allCardholders)
        {
            Logger.LogInformation("Cardholder Name: {CardholderName}", item.Name);
        }

        Logger.LogInformation(
            "Number of cardholders in Group: {CardholderCount}",
            allCardholders.Count);

        var credentialsInUse = await ProcessCredentialsAsyncNew(allCardholders)
            .IgnoreCtx();

        var usedCardholders = credentialsInUse
            .Select(credential => credential.Cardholder)
            .ToList();

        await ProcessCardholdersAsync(
            cardholdersEmployee: usedCardholders,
            allCardholders: allCardholders)
            .IgnoreCtx();
    }

    /// <summary>
    /// Processes credentials belonging to the specified cardholders and returns the active
    /// credentials configured for PTI usage.
    /// </summary>
    /// <param name="cardholders">
    /// The cardholders whose credentials will be evaluated and monitored.
    /// </param>
    /// <returns>
    /// A task that returns the active credentials that are configured for PTI usage.
    /// </returns>
    /// <remarks>
    /// This method subscribes to cardholder credential-change events and credential custom-field
    /// change events before filtering credentials by their active state and PTI usage configuration.
    /// </remarks>
    private async Task<IReadOnlyCollection<Credential>> ProcessCredentialsAsyncNew(IEnumerable<Cardholder> cardholders)
    {
        cardholders.ForEachSS(ch => ch.CredentialChanged += Cardholder_CredentialChanged);

        var credentials = cardholders
            .SelectMany(ch => ch.Credentials)
            .Select(cr => Optional(Engine.GetEntity(cr) as Credential))
            .Somes();

        foreach (var item in credentials)
        {
            var cardholder = Engine.GetEntity<Cardholder>(item.CardholderGuid);

            if (cardholder is null)
            {
                Logger.LogWarning("Cardholder could not be loaded for Credential monitoring. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", item.Guid, item.CardholderGuid);
                continue;
            }

            item.CustomFieldsChanged += (sender, e) => CredentialStateHandler.Credential_CustomFieldsChanged(sender, e, cardholder);
        }

        var activeCredentials = credentials.Where(CredentialHelper.IsCredActive).ToList();

        var credentialInUse = await activeCredentials.WhereAsync(async cred => await CredentialHelper.IsUsedLGC(cred, CF_IsUsedPTI).IgnoreCtx()).IgnoreCtx();

        return credentialInUse.ToReadOnlyCollection();
    }

    /// <summary>
    /// Processes eligible and ineligible cardholders and synchronizes their corresponding
    /// employee records with PTI.
    /// </summary>
    /// <param name="cardholdersEmployee">
    /// Cardholders that currently have credentials eligible for PTI synchronization.
    /// </param>
    /// <param name="allCardholders">
    /// All cardholders currently contained in the configured Genetec cardholder group.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous processing operation.
    /// </returns>
    /// <remarks>
    /// Duplicate eligible cardholders are removed before mapping. Eligible cardholders are added
    /// or updated in PTI, while cardholders that are not part of the eligible set are deactivated.
    /// </remarks>
    private async Task ProcessCardholdersAsync(
    List<Cardholder> cardholdersEmployee,
    IReadOnlyCollection<Cardholder> allCardholders)
    {
        try
        {
            var processedCardholderGuids = new Generic.HashSet<Guid>();
            var employeeCardholders = new List<Cardholder>();

            Logger.LogInformation("Cardholders received for processing: {Count}", cardholdersEmployee.Count);

            foreach (var cardholder in cardholdersEmployee)
            {
                Logger.LogInformation("Processing Cardholder. CardholderGuid: {CardholderGuid}, Name: {Name}", cardholder.Guid, cardholder.Name);

                var added = processedCardholderGuids.Add(cardholder.Guid);

                Logger.LogInformation("Cardholder Guid added to processed set: {Added}. CardholderGuid: {CardholderGuid}", added, cardholder.Guid);

                if (!added)
                {
                    continue;
                }

                employeeCardholders.Add(cardholder);
            }

            Logger.LogInformation("Employee Cardholders after duplicate filtering: {Count}", employeeCardholders.Count);

            var employees = await MapCardholderToEntity(employeeCardholders)
                .IgnoreCtx();

            var employeeCardholderGuids = new Generic.HashSet<Guid>(
                employeeCardholders.Select(cardholder => cardholder.Guid));

            var nonEmployeeCardholderGuids = allCardholders
                .Where(cardholder => !employeeCardholderGuids.Contains(cardholder.Guid))
                .Select(cardholder => cardholder.Guid)
                .ToList();

            await AddEmployeesToPTI(employees)
          .IgnoreCtx();

            await DeactivateEmployeesFromPTI(nonEmployeeCardholderGuids)
                .IgnoreCtx();
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error processing cardholders");
        }
    }

    /// <summary>
    /// Adds or updates the specified employees in PTI.
    /// </summary>
    /// <param name="employees">
    /// The employee entities to add or update.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous PTI synchronization operation.
    /// </returns>
    private async Task AddEmployeesToPTI(
      IReadOnlyCollection<EmployeeEntity> employees)
    {
        Logger.LogInformation(
            "Number of cardholders to add or update in PTI: {EmployeeCount}",
            employees.Count);

        await EmployeeManager
         .AddOrUpdateEmployeesFirstAsync(employees)
         .IgnoreCtx();
    }

    /// <summary>
    /// Deactivates PTI employees associated with the specified cardholder identifiers.
    /// </summary>
    /// <param name="nonEmployees">
    /// The identifiers of cardholders that should no longer be active employees in PTI.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous deactivation operation.
    /// </returns>
    /// <remarks>
    /// Each cardholder is loaded from Genetec and mapped to an inactive employee representation
    /// before the employee is deactivated in PTI.
    /// </remarks>
    private async Task DeactivateEmployeesFromPTI(IReadOnlyCollection<Guid> nonEmployees)
    {
        Logger.LogInformation("Number of cardholders to deactivate from PTI: {EmployeeCount}", nonEmployees.Count);

        foreach (var item in nonEmployees)
        {
            var cardholder = Engine.GetEntity<Cardholder>(item);

            if (cardholder is null)
            {
                Logger.LogWarning("Cardholder could not be loaded for deactivation. CardholderGuid: {CardholderGuid}", item);
                continue;
            }

            var cardholders = new List<Cardholder> { cardholder };
            var employees = await MapCardholderToInactiveEntity(cardholders).IgnoreCtx();
            var employee = employees.FirstOrDefault();

            if (employee is null)
            {
                Logger.LogWarning("Employee could not be built for deactivation. CardholderGuid: {CardholderGuid}", item);
                continue;
            }

            await EmployeeManager.TryForceDeactivateEmployeeAsync(employee).IgnoreCtx();
        }
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Gets the first active credential for the specified cardholder that is configured for PTI usage.
    /// </summary>
    /// <param name="cardholder">
    /// The cardholder whose credentials will be evaluated.
    /// </param>
    /// <param name="engine">
    /// The Genetec engine used to load credential entities.
    /// </param>
    /// <param name="cf_IsUsedByLGC">
    /// The custom-field definition used to determine whether a credential is enabled for PTI usage.
    /// </param>
    /// <returns>
    /// A task that returns the first matching credential as an <see cref="Option{T}"/>,
    /// or <see cref="Option{T}.None"/> when no matching credential exists.
    /// </returns>
    internal static async Task<Option<Credential>> GetCredentialAsync(
        Cardholder cardholder, IEngine engine, SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO cf_IsUsedByLGC)
    {
        var creds = await cardholder
            .Credentials
            .Select(engine.GetEntity<Credential>)
            .Where(CredentialHelper.IsCredActive)
            .WhereAsync(async cr => await CredentialHelper.IsUsedLGC(cr, cf_IsUsedByLGC).IgnoreCtx())
            .IgnoreCtx();

        return creds.HeadOrNone();
    }

    /// <summary>
    /// Maps active cardholders with valid PTI credentials and department information
    /// to active employee entities.
    /// </summary>
    /// <param name="cardholders">
    /// The cardholders to map to active PTI employees.
    /// </param>
    /// <returns>
    /// A task that returns the successfully mapped active employee entities.
    /// </returns>
    /// <remarks>
    /// Inactive cardholders are excluded. Cardholders without a valid PTI credential or a configured
    /// department are not included in the returned collection.
    /// </remarks>
    public async Task<IReadOnlyCollection<EmployeeEntity>> MapCardholderToEntity(IEnumerable<Cardholder> cardholders)
    {
        var activeCardholders = cardholders
           .Where(ch => CardholderStateSS.Active.AreEqual(ch.Status.State)).ToList();

        var entities = new List<EmployeeEntity>();

        foreach (var ch in activeCardholders)
        {
            var credential = await GetCredentialAsync(ch, Engine, CF_IsUsedPTI).IgnoreCtx();

            var departmentValue = ch.GetCustomFields()
         .FirstOrDefault(cf =>
             cf.CustomField.Guid == CF_Department.CustomFieldId)?
         .Value?
         .ToString();

            Option<string> department = string.IsNullOrWhiteSpace(departmentValue)
                ? Option<string>.None
                : Prelude.Some(departmentValue);

            if (department.IsNone)
            {
                Logger.Error(
                    $"Custom field {CF_Department.Name} is not set on cardholder: {ch.Name}. " +
                    "Value must be set before adding the cardholder to PTI");

                continue;
            }

            var data = from cred in credential
                       from dept in department
                       select (cred, dept);

            var chs = data.Bind(dt => CardholderHelper.NewActive(Logger, dt.cred, dt.dept));
            entities.AddRange(chs);

        }
        return entities.ToList();

    }

    /// <summary>
    /// Maps the specified cardholders to inactive employee entities.
    /// </summary>
    /// <param name="cardholders">
    /// The cardholders to map to inactive PTI employee representations.
    /// </param>
    /// <returns>
    /// A completed task containing the successfully mapped inactive employee entities.
    /// </returns>
    public Task<IReadOnlyCollection<EmployeeEntity>> MapCardholderToInactiveEntity(IEnumerable<Cardholder> cardholders)
    {
        var entities = new List<EmployeeEntity>();

        foreach (var cardholder in cardholders)
        {
            var employee = CardholderHelper.NewInactive(Logger, cardholder);
            employee.IfSome(entities.Add);
        }

        return Task.FromResult<IReadOnlyCollection<EmployeeEntity>>(entities);
    }

    #endregion Private Methods
}