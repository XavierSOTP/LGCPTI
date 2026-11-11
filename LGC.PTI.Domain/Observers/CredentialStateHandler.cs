using Microsoft.Extensions.Logging;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Manages credential state transitions and credential custom-field changes that affect
/// employee synchronization with PTI.
/// </summary>
/// <remarks>
/// The handler evaluates whether credentials are active and enabled for PTI, determines whether
/// a cardholder has another valid PTI credential, and activates, updates, or deactivates the
/// corresponding employee as required.
/// </remarks>
public sealed class CredentialStateHandler : ICredentialStateHandler
{
    private Func<LanguageExt.Unit> _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialStateHandler"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used for diagnostic and operational messages.
    /// </param>
    /// <param name="employeeManager">
    /// The employee manager responsible for synchronizing employee state with PTI.
    /// </param>
    /// <param name="engine">
    /// The Genetec SDK engine used to retrieve credential and cardholder entities.
    /// </param>
    public CredentialStateHandler(ILogger<CredentialStateHandler> logger, IEmployeeManager employeeManager, IEngine engine)
    {
        Logger = logger;
        EmployeeManager = employeeManager;
        _isInitialized = () => throw new InvalidOperationException("CredentialStateHandler is not initialized");
        Engine = engine;
    }

    /// <summary>
    /// Gets or sets the custom-field definition used to determine whether a credential is enabled for PTI.
    /// </summary>
    private SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO IsUseCustomField { get; set; }

    /// <summary>
    /// Gets or sets the custom-field definition containing the cardholder department.
    /// </summary>
    private SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO DepartmentCustomField { get; set; }

    /// <summary>
    /// Gets the logger used for diagnostic and operational messages.
    /// </summary>
    public ILogger<CredentialStateHandler> Logger { get; }

    /// <summary>
    /// Gets the Genetec SDK engine used to retrieve entities.
    /// </summary>
    public IEngine Engine { get; }

    /// <summary>
    /// Gets the employee manager responsible for PTI employee operations.
    /// </summary>
    public IEmployeeManager EmployeeManager { get; }

    /// <summary>
    /// Initializes the handler with the custom-field definitions required to process credential changes.
    /// </summary>
    /// <param name="isUseCustomField">
    /// The custom-field definition used to determine whether a credential is enabled for PTI.
    /// </param>
    /// <param name="departmentCustomField">
    /// The custom-field definition containing the cardholder department.
    /// </param>
    /// <returns>
    /// A <see cref="LanguageExt.Unit"/> value indicating that initialization completed.
    /// </returns>
    /// <remarks>
    /// This method must be called before credential custom-field events are processed.
    /// </remarks>
    public LanguageExt.Unit Initialize(SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO isUseCustomField, SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO departmentCustomField)
    {

        IsUseCustomField = isUseCustomField;
        DepartmentCustomField = departmentCustomField;

        Logger.LogInformation("CredentialStateHandler initialized. CustomFieldName: {CustomFieldName}, CustomFieldId: {CustomFieldId}", IsUseCustomField.Name, IsUseCustomField.CustomFieldId);

        _isInitialized = () => LanguageExt.Unit.Default;

        return LanguageExt.Unit.Default;
    }

    /// <summary>
    /// Determines whether the specified cardholder has another active credential that is enabled for PTI.
    /// </summary>
    /// <param name="cardholder">
    /// The cardholder whose credentials will be evaluated.
    /// </param>
    /// <param name="currentCredential">
    /// The current credential, which is excluded from the search.
    /// </param>
    /// <returns>
    /// A task that returns <see langword="true"/> when another active PTI-enabled credential exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public async Task<bool> HasAnotherActivePtiCredentialAsync(Cardholder cardholder, Credential currentCredential)
    {
        foreach (var credentialGuid in cardholder.Credentials)
        {
            if (credentialGuid == currentCredential.Guid)
            {
                continue;
            }

            var credential = Engine.GetEntity<Credential>(credentialGuid);

            if (credential is null)
            {
                continue;
            }

            if (!CredentialHelper.IsCredActive(credential))
            {
                continue;
            }

            var isUsed = await CredentialHelper.IsUsedLGC(credential, IsUseCustomField).IgnoreCtx();

            if (!isUsed)
            {
                continue;
            }

            Logger.LogInformation("Another active PTI Credential was found. CardholderGuid: {CardholderGuid}, CredentialGuid: {CredentialGuid}", cardholder.Guid, credential.Guid);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles credential custom-field changes that affect whether the associated employee
    /// should be active in PTI.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the custom-field change event. Processing continues only when
    /// this object is a <see cref="Credential"/>.
    /// </param>
    /// <param name="e">
    /// The event arguments associated with the custom-field change.
    /// </param>
    /// <param name="cardholder">
    /// The cardholder associated with the changed credential.
    /// </param>
    /// <remarks>
    /// When the credential is enabled for PTI, the method verifies that both the cardholder and
    /// credential are active and that the department field is populated before adding or updating
    /// the employee. When the credential is no longer enabled for PTI, the employee is deactivated
    /// only if no other active PTI credential exists for the cardholder.
    /// </remarks>
    public async void Credential_CustomFieldsChanged(object sender, EventArgs e, Cardholder cardholder)
    {
        try
        {
            _isInitialized();

            if (sender is not Credential credential)
            {
                return;
            }

            Logger.LogInformation("Credential custom fields changed. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, cardholder.Guid);

            var isUsed = await CredentialHelper.IsUsedLGC(credential, IsUseCustomField).IgnoreCtx();

            if (isUsed)
            {
                if (!CardholderStateSS.Active.AreEqual(cardholder.Status.State))
                {
                    Logger.LogInformation("Employee activation skipped because Cardholder is inactive. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                if (!CredentialHelper.IsCredActive(credential))
                {
                    Logger.LogInformation("Employee activation skipped because Credential is inactive. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, cardholder.Guid);
                    return;
                }

                var departmentValue = cardholder.GetCustomFields().FirstOrDefault(cf => cf.CustomField.Guid == DepartmentCustomField.CustomFieldId)?.Value?.ToString();

                if (string.IsNullOrWhiteSpace(departmentValue))
                {
                    Logger.LogWarning("Cardholder Department is empty. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                var employee1 = CardholderHelper.NewActive(Logger, credential, departmentValue);

                if (employee1.IsNone)
                {
                    Logger.LogWarning("Employee could not be built for activation. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, cardholder.Guid);
                    return;
                }

                await EmployeeManager.AddOrUpdateEmployeeAsync(employee1.ForceGetValue()).IgnoreCtx();

                Logger.LogInformation("Employee activation or update requested. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, cardholder.Guid);

                return;
            }

            var hasAnotherCredential = await HasAnotherActivePtiCredentialAsync(cardholder, credential).IgnoreCtx();

            if (hasAnotherCredential)
            {
                Logger.LogInformation("Employee will remain active because another active PTI Credential exists. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                return;
            }

            Logger.LogInformation("No other active PTI Credential found. Employee will be deactivated. CardholderGuid: {CardholderGuid}", cardholder.Guid);


            var employee = CardholderHelper.NewInactive(Logger, cardholder);

            if (employee.IsNone)
            {
                Logger.LogWarning("Employee could not be built for deactivation. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                return;
            }

            await EmployeeManager.TryForceDeactivateEmployeeAsync(employee.ForceGetValue()).IgnoreCtx();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing Credential custom field change. CardholderGuid: {CardholderGuid}", cardholder?.Guid);
        }
    }

    /// <summary>
    /// Handles a credential custom-field change and deactivates the associated employee
    /// when the credential is no longer enabled for PTI.
    /// </summary>
    /// <param name="sender">
    /// The object that raised the custom-field change event. Processing continues only when
    /// this object is a <see cref="Credential"/>.
    /// </param>
    /// <param name="e">
    /// The event arguments associated with the custom-field change.
    /// </param>
    /// <param name="cardholder">
    /// The cardholder associated with the changed credential.
    /// </param>
    /// <remarks>
    /// The method validates that the handler has been initialized, evaluates the credential's
    /// PTI usage custom field, and deactivates the corresponding employee when the credential
    /// is no longer enabled for PTI.
    /// </remarks>
    public async void Entity_CustomFieldsChange(object sender, EventArgs e, Cardholder cardholder)
    {
        try
        {
            _isInitialized();

            Logger.LogEnteringMethod();

            if (sender is not Credential credentialEntity)
            {
                Logger.LogWarning("Credential custom field change event received with invalid sender. SenderType: {SenderType}", sender?.GetType().FullName ?? "null");
                return;
            }

            Logger.LogInformation("Credential custom fields changed. CredentialGuid: {CredentialGuid}, CredentialName: {CredentialName}, CardholderGuid: {CardholderGuid}", credentialEntity.Guid, credentialEntity.Name, cardholder?.Guid);

            var credential = Optional(credentialEntity);

            var isInUse = await credential.KeepAsSomeIfAsync(async cr => await CredentialHelper.IsUsedLGC(cr, IsUseCustomField).IgnoreCtx()).IgnoreCtx();

            Logger.LogInformation("Credential PTI usage evaluated. CredentialGuid: {CredentialGuid}, IsInUse: {IsInUse}", credentialEntity.Guid, isInUse.IsSome);

            if (isInUse.IsSome)
            {
                Logger.LogWarning("Credential is enabled for PTI. No deactivation required. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credentialEntity.Guid, cardholder?.Guid);
                return;
            }

            if (cardholder is null)
            {
                Logger.LogWarning("Employee could not be built for deactivation because Cardholder is null. CredentialGuid: {CredentialGuid}", credentialEntity.Guid);
                return;
            }

            var employee = CardholderHelper.NewInactive(Logger, cardholder);

            if (employee.IsNone)
            {
                Logger.LogWarning("Employee could not be built for deactivation. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credentialEntity.Guid, cardholder.Guid);
                return;
            }

            var employeeEntity = employee.ForceGetValue();

            Logger.LogInformation("Credential is no longer enabled for PTI. Deactivating Employee. CredentialGuid: {CredentialGuid}, EmployeeNumber: {EmployeeNumber}", credentialEntity.Guid, employeeEntity.EmployeeNumber);

            await EmployeeManager.TryForceDeactivateEmployeeAsync(employeeEntity).IgnoreCtx();

            Logger.LogInformation("Employee deactivation completed. CredentialGuid: {CredentialGuid}, EmployeeNumber: {EmployeeNumber}", credentialEntity.Guid, employeeEntity.EmployeeNumber);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing Credential custom field change. CardholderGuid: {CardholderGuid}", cardholder?.Guid);
        }
        finally
        {
            Logger.LogLeavingMethod();
        }
    }
}