using LGC.PTI.Shared.Common;
using Microsoft.Extensions.Logging;
using R3;
using SoftSolute.Plugin.Common.EntityDefinitions;
using SoftSolute.Plugin.Exe.EntityObservers;
using SoftSolute.Utiities.Plugin.Adapters;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Provides the entity type handled by a typed entity observer.
/// </summary>
public interface ITypedEntityObserver
{
    /// <summary>
    /// Gets the Genetec entity type monitored by the observer.
    /// </summary>
    EntityType ObserverType { get; }
}

/// <summary>
/// Observes Credential entities in Genetec Security Center and synchronizes
/// credential-related changes with PTI employee state.
/// </summary>
/// <remarks>
/// The observer monitors credential creation, invalidation, removal, and custom-field changes,
/// and coordinates employee synchronization through the cardholder observer and credential state handler.
/// </remarks>
public sealed class CredentialObserver :
    EntityObserver,
    ITypedEntityObserver
{
    /// <summary>
    /// Defines the unique identifier assigned to the credential observer.
    /// </summary>
    private static readonly Guid CredentialObserverGuid =
        new("A8C27317-3E51-4D5A-98D4-A0B1F08A09C4");

    /// <summary>
    /// Determines whether an entity update originated externally rather than from a local update.
    /// </summary>
    private static readonly Func<EntitiesUpdateEventArgs, bool> TriggerByUsers =
        static r => !r.IsLocalUpdate;

    private int _eventRegistrationCount;

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialObserver"/> class.
    /// </summary>
    /// <param name="engine">
    /// The Genetec SDK engine used to load entities and subscribe to entity events.
    /// </param>
    /// <param name="logger">
    /// The logger used for diagnostic and operational messages.
    /// </param>
    /// <param name="employeeManager">
    /// The employee manager responsible for synchronizing employee data with PTI.
    /// </param>
    /// <param name="cardholderObserver">
    /// The cardholder observer used to map Genetec cardholders to PTI employee entities.
    /// </param>
    /// <param name="credentialStateHandler">
    /// The handler responsible for processing credential state and custom-field changes.
    /// </param>
    /// <param name="appInitializedNotifier">
    /// The notifier used to start observer initialization after application initialization completes.
    /// </param>
    public CredentialObserver(
        IEngine engine,
        ILogger<CredentialObserver> logger,
        IEmployeeManager employeeManager,
        ICardholderObserver cardholderObserver,
        ICredentialStateHandler credentialStateHandler,
        IAppInitializedNotifierPlugin appInitializedNotifier)
        : base(
            engine,
            logger,
            CredentialObserverGuid)
    {
        EmployeeManager = employeeManager;
        CardholderObserver = cardholderObserver;
        CredentialStateHandler = credentialStateHandler;

        appInitializedNotifier.Initialized +=
            OnAppInitialized;

        Logger.LogInformation(
            "CredentialObserver created. Instance: {Instance}",
            GetHashCode());
    }

    #endregion Public Constructors

    #region Properties

    /// <summary>
    /// Gets or sets the custom-field definition used to determine whether a credential is enabled for PTI.
    /// </summary>
    private SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO CF_IsUsedLGC { get; set; }

    /// <summary>
    /// Gets the employee manager responsible for PTI employee operations.
    /// </summary>
    private IEmployeeManager EmployeeManager { get; }

    /// <summary>
    /// Gets the cardholder observer used to map cardholders to employee entities.
    /// </summary>
    public ICardholderObserver CardholderObserver { get; }

    /// <summary>
    /// Gets the credential state handler used to process credential changes.
    /// </summary>
    public ICredentialStateHandler CredentialStateHandler { get; }

    /// <summary>
    /// Gets the internal entity type representation for Genetec credentials.
    /// </summary>
    internal EntityTypeSS CredentialType =>
        EntityTypeSS.Credential;

    /// <summary>
    /// Gets the Genetec entity type monitored by this observer.
    /// </summary>
    public EntityType ObserverType =>
        EntityType.Credential;

    /// <summary>
    /// Gets or sets the action responsible for releasing managed resources.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this resource-release action is not implemented by this observer.
    /// </exception>
    protected override Action ReleaseManagedResources
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    /// <summary>
    /// Gets or sets the action responsible for releasing unmanaged resources.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this resource-release action is not implemented by this observer.
    /// </exception>
    protected override Action ReleaseUnmanagedResources
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    #endregion Properties

    #region Initialization

    /// <summary>
    /// Initializes the credential observer after application initialization completes.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel initialization.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous initialization operation.
    /// </returns>
    private async Task OnAppInitialized(
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation(
                "CredentialObserver initialization started.");

            cancellationToken.ThrowIfCancellationRequested();

            var result = await ConfigureObserverAsync()
                .RunAsync()
                .ConfigureAwait(false);

            if (result.IsFail)
            {
                Logger.LogError(

                    "CredentialObserver initialization failed.");

                return;
            }

            Logger.LogInformation(
                "CredentialObserver initialized successfully.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Logger.LogInformation(
                "CredentialObserver initialization cancelled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "CredentialObserver initialization failed.");
        }
    }

    /// <summary>
    /// Configures the credential observer by resolving required custom fields,
    /// prefetching credential entities, and registering entity event handlers.
    /// </summary>
    /// <returns>
    /// An asynchronous LanguageExt effect representing the observer configuration operation.
    /// </returns>
    /// <remarks>
    /// The PTI usage custom field is loaded from the credential definition before event registration begins.
    /// </remarks>
    internal Aff<LanguageExt.Unit> ConfigureObserverAsync()
    {
        return LanguageExtAffExtensions.Aff(async () =>
        {
            try
            {
                var customFields =
                    Engine.GetCustomFields(
                        EntityTypeSS.Credential);

                var credentialDefinition =
                    CredentialDefinition
                        .New()
                        .Populate(customFields);

                CF_IsUsedLGC =
                    credentialDefinition.IsUsedCredential;

                Logger.LogInformation(
                    "Credential custom field loaded. Name: {Name}, CustomFieldId: {CustomFieldId}",
                    CF_IsUsedLGC.Name,
                    CF_IsUsedLGC.CustomFieldId);

                EntityRepository.PrefetchEntity(
                    Engine,
                    CredentialType);

                await RegisterToEntityEvents()
                    .RunAsync()
                    .ConfigureAwait(false);

                return Prelude.unit;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Failed to configure CredentialObserver.");

                throw;
            }
        });
    }

    #endregion Initialization

    #region Entity Events

    /// <summary>
    /// Filters an entity update event to include only credential entity updates.
    /// </summary>
    /// <param name="ev">
    /// The entity update event containing one or more updated entities.
    /// </param>
    /// <returns>
    /// A list containing only updates for the entity type monitored by this observer.
    /// </returns>
    private List<EntityUpdateInfo> OnlyCred(
        EntitiesUpdateEventArgs ev)
    {
        return
        [
            .. ev.Entities.Where(
                r =>
                    r.EntityType == ObserverType)
        ];
    }

    /// <summary>
    /// Registers handlers for credential added, removed, and invalidated events.
    /// </summary>
    /// <returns>
    /// An asynchronous LanguageExt effect representing the event-registration operation.
    /// </returns>
    /// <remarks>
    /// Local entity updates are ignored. A warning is logged when registration occurs more than once.
    /// </remarks>
    private Aff<LanguageExt.Unit> RegisterToEntityEvents()
    {
        return LanguageExtAffExtensions.Aff(async () =>
        {
            var registrationCount =
                Interlocked.Increment(
                    ref _eventRegistrationCount);

            if (registrationCount > 1)
            {
                Logger.LogWarning(
                    "Credential events are being registered more than once. RegistrationCount: {RegistrationCount}",
                    registrationCount);
            }
            else
            {
                Logger.LogInformation(
                    "Registering Credential entity events.");
            }

            EntitiesAddedEvents
                .Where(TriggerByUsers)
                .Select(OnlyCred)
                .Where(r =>
                    r.Count > 0)
                .SelectMany(e =>
                    e.ToObservable())
                .SubscribeTask(
                    OnEntitiesAdded());

            EntitiesRemovedEvents
                .Where(TriggerByUsers)
                .Select(OnlyCred)
                .SelectMany(e =>
                    e.ToObservable())
                .SubscribeTask(
                    OnEntitiesRemoved());

            EntitiesInvalidatedEvents
                .Where(TriggerByUsers)
                .Select(OnlyCred)
                .SelectMany(e =>
                    e.ToObservable())
                .SubscribeTask(
                    OnEntitiesInvalidated());

            Logger.LogInformation(
                "Credential entity events registered.");

            return Prelude.unit;
        });
    }

    /// <summary>
    /// Creates the handler used when a credential entity is added.
    /// </summary>
    /// <returns>
    /// A callback that processes the added credential and supports cancellation.
    /// </returns>
    /// <remarks>
    /// The handler starts monitoring credential custom-field changes. When the credential is enabled
    /// for PTI, the associated cardholder is mapped to an employee and synchronized with PTI.
    /// </remarks>
    private Func<EntityUpdateInfo, CancellationToken, Task> OnEntitiesAdded()
    {
        return async (update, cancellationToken) =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (update.EntityType != CredentialType)
                {
                    return;
                }

                var credential = Engine.GetEntity(update.EntityGuid, true) as Credential;

                if (credential is null)
                {
                    Logger.LogWarning("Added Credential could not be loaded. CredentialGuid: {CredentialGuid}", update.EntityGuid);
                    return;
                }

                Logger.LogInformation("Credential added. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, credential.CardholderGuid);

                _ = credential.Status.State;

                var cardholder = Engine.GetEntity<Cardholder>(credential.CardholderGuid);

                if (cardholder is null)
                {
                    Logger.LogWarning("Cardholder for added Credential could not be loaded. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, credential.CardholderGuid);
                    return;
                }

                credential.CustomFieldsChanged += (sender, e) => CredentialStateHandler.Credential_CustomFieldsChanged(sender, e, cardholder);

                Logger.LogInformation("Credential monitoring started. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, cardholder.Guid);

                var isUsed = await CredentialHelper.IsUsedLGC(credential, CF_IsUsedLGC).IgnoreCtx();

                if (!isUsed)
                {
                    Logger.LogInformation("Added Credential is not enabled for PTI. Employee will not be sent. CredentialGuid: {CredentialGuid}", credential.Guid);
                    return;
                }

                var cardholders = new List<Cardholder> { cardholder };
                var employees = await CardholderObserver.MapCardholderToEntity(cardholders).IgnoreCtx();
                var employee = employees.FirstOrDefault();

                if (employee is null)
                {
                    Logger.LogWarning("Employee could not be built from added Credential. Verify Credential status and Department. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}", credential.Guid, cardholder.Guid);
                    return;
                }

                Logger.LogInformation("Added Credential is enabled for PTI and Cardholder has a valid Department. Sending Employee to PTI. CredentialGuid: {CredentialGuid}, EmployeeNumber: {EmployeeNumber}", credential.Guid, employee.EmployeeNumber);

                await EmployeeManager.AddOrUpdateEmployeeAsync(employee).IgnoreCtx();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing added Credential. CredentialGuid: {CredentialGuid}", update.EntityGuid);
            }

            await Task.CompletedTask;
        };
    }

    //private Func<EntityUpdateInfo, CancellationToken, Task> OnEntitiesAdded()
    //{
    //    return async (update, cancellationToken) =>
    //    {
    //        try
    //        {
    //            cancellationToken.ThrowIfCancellationRequested();

    //            if (update.EntityType != CredentialType)
    //            {
    //                return;
    //            }

    //            var credential =
    //                Engine.GetEntity(
    //                    update.EntityGuid,
    //                    true) as Credential;

    //            if (credential is null)
    //            {
    //                Logger.LogWarning(
    //                    "Added Credential could not be loaded. CredentialGuid: {CredentialGuid}",
    //                    update.EntityGuid);

    //                return;
    //            }

    //            Logger.LogInformation(
    //                "Credential added. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
    //                credential.Guid,
    //                credential.CardholderGuid);

    //            _ = credential.Status.State;

    //            var cardholder =
    //                Engine.GetEntity<Cardholder>(
    //                    credential.CardholderGuid);

    //            if (cardholder is null)
    //            {
    //                Logger.LogWarning(
    //                    "Cardholder for added Credential could not be loaded. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
    //                    credential.Guid,
    //                    credential.CardholderGuid);

    //                return;
    //            }

    //            var cardholders =
    //                new List<Cardholder>
    //                {
    //                    cardholder
    //                };

    //            var employees =
    //                await CardholderObserver
    //                    .MapCardholderToEntity(
    //                        cardholders)
    //                    .IgnoreCtx();

    //            var employee =
    //                employees.FirstOrDefault();

    //            if (employee is null)
    //            {
    //                Logger.LogWarning(
    //                    "Employee could not be mapped for added Credential. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
    //                    credential.Guid,
    //                    cardholder.Guid);

    //                return;
    //            }

    //            credential.CustomFieldsChanged +=
    //                (sender, e) =>
    //                    CredentialStateHandler
    //                        .Entity_CustomFieldsChanged(
    //                            sender,
    //                            e,
    //                            employee);

    //            Logger.LogInformation(
    //                "Credential monitoring started. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
    //                credential.Guid,
    //                cardholder.Guid);
    //        }
    //        catch (OperationCanceledException)
    //            when (cancellationToken.IsCancellationRequested)
    //        {
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.LogError(
    //                ex,
    //                "Error processing added Credential. CredentialGuid: {CredentialGuid}",
    //                update.EntityGuid);
    //        }
    //    };
    //}

    /// <summary>
    /// Creates the handler used when a credential entity is invalidated.
    /// </summary>
    /// <returns>
    /// A callback that processes the invalidated credential and supports cancellation.
    /// </returns>
    /// <remarks>
    /// Only credentials enabled for PTI are considered. When such a credential becomes inactive,
    /// the employee is deactivated only when the associated cardholder does not have another active PTI credential.
    /// </remarks>
    private Func<EntityUpdateInfo, CancellationToken, Task> OnEntitiesInvalidated()
    {
        return async (update, cancellationToken) =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (update.EntityType != CredentialType)
                {
                    return;
                }

                var credential =
                    Engine.GetEntity<Credential>(
                        update.EntityGuid);

                if (credential is null)
                {
                    Logger.LogWarning(
                        "Modified Credential could not be loaded. CredentialGuid: {CredentialGuid}",
                        update.EntityGuid);

                    return;
                }

                var isUsedLgc =
                    await CredentialHelper
                        .IsUsedLGC(
                            credential,
                            CF_IsUsedLGC)
                        .IgnoreCtx();

                if (!isUsedLgc)
                {
                    return;
                }

                if (CredentialHelper.IsCredActive(
                    credential))
                {
                    return;
                }

                Logger.LogInformation(
                    "PTI Credential became inactive. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
                    credential.Guid,
                    credential.CardholderGuid);

                var cardholder =
                    Engine.GetEntity<Cardholder>(
                        credential.CardholderGuid);

                if (cardholder is null)
                {
                    Logger.LogWarning(
                        "Cardholder could not be loaded for inactive Credential. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
                        credential.Guid,
                        credential.CardholderGuid);

                    return;
                }

                var hasAnotherCredential = await CredentialStateHandler.HasAnotherActivePtiCredentialAsync(cardholder, credential).IgnoreCtx();

                if (hasAnotherCredential)
                {
                    Logger.LogInformation("Employee will remain active because another active PTI Credential exists. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                var cardholders =
                    new List<Cardholder>
                    {
                        cardholder
                    };

                var employees =
                    await CardholderObserver
                        .MapCardholderToInactiveEntity(
                            cardholders)
                        .IgnoreCtx();

                var employee =
                    employees.FirstOrDefault();

                if (employee is null)
                {
                    Logger.LogWarning(
                        "Employee could not be mapped for inactive Credential. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
                        credential.Guid,
                        cardholder.Guid);

                    return;
                }

                await EmployeeManager
                    .TryForceDeactivateEmployeeAsync(
                        employee)
                    .IgnoreCtx();

                Logger.LogInformation(
                    "Employee deactivated because Credential became inactive. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
                    credential.Guid,
                    cardholder.Guid);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error processing modified Credential. CredentialGuid: {CredentialGuid}",
                    update.EntityGuid);
            }
        };
    }

    /// <summary>
    /// Creates the handler used when a credential entity is removed.
    /// </summary>
    /// <returns>
    /// A callback that processes the removed credential and supports cancellation.
    /// </returns>
    /// <remarks>
    /// When the associated cardholder has no other active PTI credential,
    /// the cardholder is mapped to an inactive employee and deactivated in PTI.
    /// </remarks>
    private Func<EntityUpdateInfo, CancellationToken, Task> OnEntitiesRemoved()
    {
        return async (update, cancellationToken) =>
        {
            try
            {

                PTITodoCode.Instance.CodeReviewRequired("Review Code");
                cancellationToken.ThrowIfCancellationRequested();

                if (update.EntityType != CredentialType)
                {
                    return;
                }

                Logger.LogInformation(
                    "Credential removed. CredentialGuid: {CredentialGuid}",
                    update.EntityGuid);

                var credential =
                    Engine.GetEntity<Credential>(
                        update.EntityGuid);

                if (credential is null)
                {
                    Logger.LogWarning(
                        "Removed Credential could not be loaded. CredentialGuid: {CredentialGuid}",
                        update.EntityGuid);

                    return;
                }

                var cardholder =
                    Engine.GetEntity<Cardholder>(
                        credential.CardholderGuid);

                if (cardholder is null)
                {
                    Logger.LogWarning(
                        "Cardholder could not be loaded for removed Credential. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
                        credential.Guid,
                        credential.CardholderGuid);

                    return;
                }
                var hasAnotherCredential = await CredentialStateHandler.HasAnotherActivePtiCredentialAsync(cardholder, credential).IgnoreCtx();

                if (hasAnotherCredential)
                {
                    Logger.LogInformation("Employee will remain active because another active PTI Credential exists. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }
                var cardholders =
                    new List<Cardholder>
                    {
                        cardholder
                    };

                var employees =
                    await CardholderObserver
                        .MapCardholderToInactiveEntity(
                            cardholders)
                        .IgnoreCtx();

                var employee =
                    employees.FirstOrDefault();

                if (employee is null)
                {
                    Logger.LogWarning(
                        "Employee could not be mapped for removed Credential. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
                        credential.Guid,
                        cardholder.Guid);

                    return;
                }

                await EmployeeManager
                    .TryForceDeactivateEmployeeAsync(
                        employee)
                    .IgnoreCtx();

                Logger.LogInformation(
                    "Employee deactivated because Credential was removed. CredentialGuid: {CredentialGuid}, CardholderGuid: {CardholderGuid}",
                    credential.Guid,
                    cardholder.Guid);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error processing removed Credential. CredentialGuid: {CredentialGuid}",
                    update.EntityGuid);
            }
        };
    }

    #endregion Entity Events
}