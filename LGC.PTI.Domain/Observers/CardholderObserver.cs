using Genetec.Sdk.Entities.CustomFields;
using LGC.PIT.Plugin.Common.Configs;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using R3;
using SoftSolute.Plugin.Common.EntityDefinitions;
using SoftSolute.Plugin.Exe.EntityObservers;
using SoftSolute.Utiities.Plugin.Adapters;

using CustomFieldDataBO = SoftSolute.Plugin.Common.EntityDefinitions.CustomFieldDataBO;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Provides mapping operations between Genetec Cardholder entities and PTI Employee entities.
/// </summary>
public interface ICardholderObserver
{
    /// <summary>
    /// Maps a collection of Genetec cardholders to active employee entities.
    /// </summary>
    /// <param name="cardholders">The cardholder entities to map.</param>
    /// <returns>A task that returns a collection of mapped employee entities.</returns>
    Task<IReadOnlyCollection<EmployeeEntity>> MapCardholderToEntity(
        IEnumerable<Cardholder> cardholders);

    /// <summary>
    /// Maps a collection of Genetec cardholders to inactive employee entities.
    /// </summary>
    /// <param name="cardholders">The cardholder entities to map to inactive status.</param>
    /// <returns>A task that returns a collection of inactive employee entities.</returns>
    Task<IReadOnlyCollection<EmployeeEntity>> MapCardholderToInactiveEntity(IEnumerable<Cardholder> cardholders);
}

/// <summary>
/// Observes and monitors changes to Cardholder entities in Genetec Security Center,
/// synchronizing them with the PTI system by mapping cardholder data to employee entities.
/// Implements typed entity observer pattern for handling cardholder-specific events.
/// </summary>
public sealed partial class CardholderObserver :
    EntityObserver,
    ITypedEntityObserver,
    ICardholderObserver
{
    #region Fields

    private static readonly Guid CardholderObserverGuid =
        new("D4B5F6E7-89A0-1234-5678-9ABCDEF02278");

    private static readonly Func<EntitiesUpdateEventArgs, bool> TriggerByUsers =
        static r => !r.IsLocalUpdate;

    private readonly IAuthCredentialStore _authTokenStore;
    private readonly IConfigurableAppSettings Settings;

    private int _eventRegistrationCount;

    #endregion Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CardholderObserver"/> class with required dependencies.
    /// Registers for cardholder entity change events and subscribes to application initialization notifications.
    /// </summary>
    /// <param name="engine">The Genetec SDK engine for entity access and event handling.</param>
    /// <param name="logger">Logger for diagnostic and informational messages.</param>
    /// <param name="employeeManager">Manager responsible for employee entity operations.</param>
    /// <param name="authTokenStore">Store for authentication credentials and tokens.</param>
    /// <param name="settings">Application configuration settings.</param>
    /// <param name="mapper">Mapper for converting between cardholder and employee entities.</param>
    /// <param name="credentialStateHandler">Handler for credential state changes.</param>
    /// <param name="appInitializedNotifier">Notifier that signals application initialization completion.</param>
    public CardholderObserver(
        IEngine engine,
        ILogger<CardholderObserver> logger,
        IEmployeeManager employeeManager,
        IAuthCredentialStore authTokenStore,
        IConfigurableAppSettings settings,
        IMapper mapper,
        ICredentialStateHandler credentialStateHandler,
        IAppInitializedNotifierPlugin appInitializedNotifier)
        : base(
            engine,
            logger,
            CardholderObserverGuid)
    {
        EmployeeManager = employeeManager;
        _authTokenStore = authTokenStore;
        Settings = settings;
        Mapper = mapper;
        CredentialStateHandler = credentialStateHandler;

        appInitializedNotifier.Initialized += OnAppInitialized;

        Logger.LogInformation(
            "CardholderObserver created. Instance: {Instance}",
            GetHashCode());
    }

    #endregion Public Constructors

    #region Properties

    /// <summary>
    /// Gets the employee manager that handles employee entity operations.
    /// </summary>
    public IEmployeeManager EmployeeManager { get; }

    /// <summary>
    /// Gets the mapper used to convert between cardholder and employee entities.
    /// </summary>
    internal IMapper Mapper { get; }

    /// <summary>
    /// Gets the credential state handler that processes credential changes.
    /// </summary>
    internal ICredentialStateHandler CredentialStateHandler { get; }

    /// <summary>
    /// Gets the entity type this observer monitors.
    /// </summary>
    internal EntityTypeSS CardholderType =>
        EntityTypeSS.Cardholder;

    /// <summary>
    /// Gets or sets the custom field definition used to identify credentials managed by PTI.
    /// </summary>
    internal CustomFieldDataBO CF_IsUsedPTI { get; set; }

    /// <summary>
    /// Gets the custom field definition containing the cardholder department.
    /// </summary>
    public CustomFieldDataBO CF_Department { get; private set; }

    /// <summary>
    /// Gets or sets the action responsible for releasing managed resources.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// Always thrown because this observer does not implement this base-class resource release action.
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
    /// Always thrown because this observer does not implement this base-class resource release action.
    /// </exception>
    protected override Action ReleaseUnmanagedResources
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the Genetec entity type observed by this instance.
    /// </summary>
    public EntityType ObserverType =>
        EntityType.Cardholder;

    #endregion Properties

    #region Initialization

    /// <summary>
    /// Initializes the cardholder observer after the application has completed initialization.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel initialization while waiting for authentication or processing synchronization.
    /// </param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <remarks>
    /// Initialization configures the observer, waits until an authentication token is available,
    /// and then performs the initial repository synchronization.
    /// </remarks>
    private async Task OnAppInitialized(
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation(
                "CardholderObserver initialization started.");

            var configureResult = await ConfigureObserverAsync()
                .RunAsync()
                .ConfigureAwait(false);

            if (configureResult.IsFail)
            {
                Logger.LogError(

                    "CardholderObserver configuration failed.");

                return;
            }

            Logger.LogInformation(
                "CardholderObserver configured successfully.");

            await WaitForTokenAsync(
                cancellationToken)
                .ConfigureAwait(false);

            Logger.LogInformation(
                "Starting Cardholder synchronization.");

            await SyncRepository()
                .IgnoreCtx();

            Logger.LogInformation(
                "Cardholder synchronization completed.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Logger.LogInformation(
                "CardholderObserver initialization cancelled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "CardholderObserver initialization failed.");
        }
    }

    /// <summary>
    /// Waits until an authentication token becomes available in the credential store.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the wait operation.
    /// </param>
    /// <returns>
    /// A task that completes when an authentication token is available.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled before a token becomes available.
    /// </exception>
    private async Task WaitForTokenAsync(
        CancellationToken cancellationToken)
    {
        if (_authTokenStore.TryGetToken().IsSome)
        {
            Logger.LogInformation(
                "Authentication token already available.");

            return;
        }

        Logger.LogInformation(
            "Authentication token not available. Waiting for token.");

        while (_authTokenStore.TryGetToken().IsNone)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(
                TimeSpan.FromMilliseconds(250),
                cancellationToken)
                .ConfigureAwait(false);
        }

        Logger.LogInformation(
            "Authentication token received. Continuing initialization.");
    }

    /// <summary>
    /// Configures the cardholder observer by resolving the required custom fields,
    /// initializing credential state handling, prefetching cardholders, and registering entity events.
    /// </summary>
    /// <returns>
    /// An asynchronous LanguageExt effect representing the observer configuration operation.
    /// </returns>
    /// <remarks>
    /// The operation resolves both credential and cardholder custom-field definitions from Genetec.
    /// Configuration fails when a required custom field cannot be resolved.
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

                var customFieldsCh =
                    Engine.GetCustomFields(
                        EntityTypeSS.Cardholder);

                var credentialDefinition =
                    PopulateLocal(
                        CredentialDefinition.New(),
                        customFields);

                var cardholderDefinition =
                    PopulateLocal(
                        CardholderDefinition.New(),
                        customFieldsCh);

                CF_IsUsedPTI =
                    credentialDefinition.IsUsedCredential;
                CF_Department =
               cardholderDefinition.Department;
                CredentialStateHandler.Initialize(
                    CF_IsUsedPTI, CF_Department);



                Logger.LogInformation(
                    "Required custom fields loaded. UsedPluginField: {UsedPluginField}, DepartmentField: {DepartmentField}",
                    CF_IsUsedPTI.CustomFieldId,
                    CF_Department.CustomFieldId);

                EntityRepository.PrefetchEntity(
                    Engine,
                    CardholderType);

                await RegisterToEntityEvents()
                    .RunAsync()
                    .ConfigureAwait(false);

                return Prelude.unit;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Failed to configure CardholderObserver.");

                throw;
            }
        });
    }

    /// <summary>
    /// Populates a local entity definition with the Genetec identifiers of its configured custom fields.
    /// </summary>
    /// <typeparam name="TDefinition">
    /// The definition type containing <see cref="CustomFieldDataBO"/> properties.
    /// </typeparam>
    /// <param name="definition">
    /// The definition instance whose custom-field identifiers will be populated.
    /// </param>
    /// <param name="valueProvider">
    /// The Genetec custom fields available for resolving the definition.
    /// </param>
    /// <returns>
    /// The supplied <paramref name="definition"/> with its custom-field identifiers populated.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a required custom field cannot be found in <paramref name="valueProvider"/>.
    /// </exception>
    private TDefinition PopulateLocal<TDefinition>(
        TDefinition definition,
        IEnumerable<CustomField> valueProvider)
        where TDefinition : class
    {
        var fields =
            valueProvider.ToList();

        foreach (var property in definition
            .GetType()
            .GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public)
            .Where(p =>
                p.PropertyType ==
                typeof(CustomFieldDataBO)))
        {
            var customFieldData =
                (CustomFieldDataBO)property.GetValue(
                    definition);

            var customField =
                fields.SingleOrDefault(f =>
                    string.Equals(
                        f.GroupName,
                        customFieldData.GroupName,
                        StringComparison.Ordinal)
                    &&
                    string.Equals(
                        f.Name,
                        customFieldData.Name,
                        StringComparison.OrdinalIgnoreCase));

            if (customField is null)
            {
                Logger.LogError(
                    "Required custom field not found. Name: {Name}, Group: {Group}",
                    customFieldData.Name,
                    customFieldData.GroupName);

                throw new InvalidOperationException(
                    $"Custom field '{customFieldData.Name}' " +
                    $"in group '{customFieldData.GroupName}' " +
                    $"was not found in the provided list.");
            }

            customFieldData.CustomFieldId =
                customField.Guid;
        }

        return definition;
    }

    #endregion Initialization

    #region Entity Events

    /// <summary>
    /// Filters an entity-update event so that only updates for the entity type observed by this instance remain.
    /// </summary>
    /// <param name="ev">
    /// The entity-update event containing the changed entities.
    /// </param>
    /// <returns>
    /// A list containing only updates whose entity type matches <see cref="ObserverType"/>.
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
    /// Registers handlers for cardholder added, removed, and invalidated entity events.
    /// </summary>
    /// <returns>
    /// An asynchronous LanguageExt effect representing the event-registration operation.
    /// </returns>
    /// <remarks>
    /// Events representing local updates are excluded. The method also tracks repeated registration
    /// attempts and writes a warning when registration occurs more than once.
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
                    "Cardholder events registered more than once. RegistrationCount: {RegistrationCount}",
                    registrationCount);
            }
            else
            {
                Logger.LogInformation(
                    "Registering Cardholder entity events.");
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
                "Cardholder entity events registered.");

            return Prelude.unit;
        });
    }

    /// <summary>
    /// Creates the asynchronous handler used when a cardholder entity is added.
    /// </summary>
    /// <returns>
    /// A callback that processes the entity update and supports cancellation.
    /// </returns>
    /// <remarks>
    /// The handler loads cardholders belonging to the configured cardholder group,
    /// subscribes to their credential-change events, and accesses state and credential
    /// information so those values are available for subsequent processing.
    /// </remarks>
    private Func<EntityUpdateInfo, CancellationToken, Task> OnEntitiesAdded()
    {
        return async (update, cancellationToken) =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (update.EntityType != CardholderType)
                {
                    return;
                }

                var cardholder =
                    Engine.GetEntity(
                        update.EntityGuid,
                        true) as Cardholder;

                if (cardholder is null)
                {
                    Logger.LogWarning(
                        "Added Cardholder could not be loaded. CardholderGuid: {CardholderGuid}",
                        update.EntityGuid);

                    return;
                }

                if (!cardholder.Parents.Contains(
                    Settings.LGCSettings.CardholderGroupId))
                {
                    return;
                }

                Logger.LogInformation(
                    "Cardholder added to monitored group. CardholderGuid: {CardholderGuid}, Name: {Name}",
                    cardholder.Guid,
                    cardholder.Name);

                cardholder.CredentialChanged +=
                    Cardholder_CredentialChanged;

                _ = cardholder.Status.State;
                _ = cardholder.Credentials;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error processing added Cardholder. CardholderGuid: {CardholderGuid}",
                    update.EntityGuid);
            }

            await Task.CompletedTask;
        };
    }

    /// <summary>
    /// Creates the asynchronous handler used when a cardholder entity is invalidated.
    /// </summary>
    /// <returns>
    /// A callback that processes the invalidated entity and supports cancellation.
    /// </returns>
    /// <remarks>
    /// The handler ensures credential monitoring remains attached, evaluates whether the
    /// corresponding PTI employee must be deactivated, and otherwise activates or updates
    /// the employee when the cardholder has a valid PTI credential and department.
    /// </remarks>
    private Func<EntityUpdateInfo, CancellationToken, Task> OnEntitiesInvalidated()
    {
        return async (update, cancellationToken) =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (update.EntityType != CardholderType)
                {
                    return;
                }

                var cardholder = Engine.GetEntity(update.EntityGuid, true) as Cardholder;

                if (cardholder is null)
                {
                    Logger.LogWarning("Invalidated Cardholder could not be loaded. CardholderGuid: {CardholderGuid}", update.EntityGuid);
                    return;
                }

                var shouldProcess = CredChangeCacheHelper.ShouldProcess(cardholder.Guid);

                if (shouldProcess.IsNone)
                {
                    return;
                }

                if (!cardholder.Parents.Contains(Settings.LGCSettings.CardholderGroupId))
                {
                    return;
                }

                cardholder.CredentialChanged -= Cardholder_CredentialChanged;
                cardholder.CredentialChanged += Cardholder_CredentialChanged;

                _ = cardholder.Status.State;
                _ = cardholder.Credentials;

                Logger.LogInformation("Cardholder monitoring ensured after invalidation. CardholderGuid: {CardholderGuid}, Name: {Name}", cardholder.Guid, cardholder.Name);

                var isDeactivated = await TryDeactivateByCardholderStateAsync(cardholder).IgnoreCtx();

                if (isDeactivated)
                {
                    Logger.LogInformation("Cardholder deactivated because of its current state. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                Logger.LogInformation("Cardholder is active. Checking if Employee should be activated in PTI. CardholderGuid: {CardholderGuid}", cardholder.Guid);

                var credential = await GetCredentialAsync(cardholder, Engine, CF_IsUsedPTI).IgnoreCtx();

                if (credential.IsNone)
                {
                    Logger.LogInformation("Cardholder is active but has no active PTI Credential. Employee activation skipped. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                var department = cardholder.GetCustomFields()
                    .FirstOrDefault(cf => cf.CustomField.Guid == CF_Department.CustomFieldId)?
                    .Value?
                    .ToString();

                if (string.IsNullOrWhiteSpace(department))
                {
                    Logger.LogWarning("Cardholder Department is empty. Employee activation skipped. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                var employee = CardholderHelper.NewActive(Logger, credential.ForceGetValue(), department);

                if (employee.IsNone)
                {
                    Logger.LogWarning("Employee could not be built for active Cardholder. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                Logger.LogInformation("Cardholder is active with a valid PTI Credential. Activating or updating Employee. CardholderGuid: {CardholderGuid}", cardholder.Guid);

                await EmployeeManager.AddOrUpdateEmployeeAsync(employee.ForceGetValue()).IgnoreCtx();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing Cardholder state change. CardholderGuid: {CardholderGuid}", update.EntityGuid);
            }
        };
    }

    /// <summary>
    /// Creates the asynchronous handler used when a cardholder entity is removed.
    /// </summary>
    /// <returns>
    /// A callback that processes the removed entity and supports cancellation.
    /// </returns>
    /// <remarks>
    /// When the removed cardholder can be loaded and mapped to an employee,
    /// the corresponding PTI employee is forcefully deactivated.
    /// </remarks>
    private Func<EntityUpdateInfo, CancellationToken, Task> OnEntitiesRemoved()
    {
        return async (update, cancellationToken) =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (update.EntityType != CardholderType)
                {
                    return;
                }

                Logger.LogInformation("Cardholder removed. CardholderGuid: {CardholderGuid}", update.EntityGuid);

                var cardholder = Engine.GetEntity<Cardholder>(update.EntityGuid);

                if (cardholder is null)
                {
                    Logger.LogWarning("Removed Cardholder could not be loaded. CardholderGuid: {CardholderGuid}", update.EntityGuid);
                    return;
                }

                var employee = CardholderHelper.NewInactive(Logger, cardholder);

                if (employee.IsNone)
                {
                    Logger.LogWarning("Employee could not be built from removed Cardholder. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                var employeeEntity = employee.ForceGetValue();

                Logger.LogInformation("Deactivating Employee because Cardholder was removed. CardholderGuid: {CardholderGuid}, EmployeeNumber: {EmployeeNumber}", cardholder.Guid, employeeEntity.EmployeeNumber);

                await EmployeeManager.TryForceDeactivateEmployeeAsync(employeeEntity).IgnoreCtx();

                Logger.LogInformation("Employee deactivation completed for removed Cardholder. CardholderGuid: {CardholderGuid}, EmployeeNumber: {EmployeeNumber}", cardholder.Guid, employeeEntity.EmployeeNumber);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing removed Cardholder. CardholderGuid: {CardholderGuid}", update.EntityGuid);
            }
        };
    }

    #endregion Entity Events

    #region Credential Events

    /// <summary>
    /// Handles credential changes for a monitored cardholder and synchronizes the corresponding PTI employee.
    /// </summary>
    /// <param name="sender">
    /// The cardholder whose credential collection changed.
    /// </param>
    /// <param name="arg">
    /// Information describing the credential change.
    /// </param>
    /// <remarks>
    /// Local updates are ignored. For externally initiated changes, the handler checks the cardholder
    /// state and PTI credential. The associated employee is deactivated when no valid PTI credential
    /// remains, or activated/updated when a valid credential and department are available.
    /// </remarks>
    public async void Cardholder_CredentialChanged(object sender, CredentialsChangedEventArg arg)
    {
        if (arg.IsLocalUpdate)
        {
            return;
        }

        try
        {
            if (sender is not Cardholder cardholder)
            {
                return;
            }

            Logger.LogInformation("Cardholder credential changed. CardholderGuid: {CardholderGuid}, Name: {Name}", cardholder.Guid, cardholder.Name);

            CredChangeCacheHelper.AddToBackOff(cardholder.Guid);

            var isDeactivated = await TryDeactivateByCardholderStateAsync(cardholder).IgnoreCtx();

            if (isDeactivated)
            {
                Logger.LogInformation("Cardholder deactivated after credential change. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                return;
            }

            var credential = await GetCredentialAsync(cardholder, Engine, CF_IsUsedPTI).IgnoreCtx();

            if (credential.IsNone)
            {
                Logger.LogInformation("No valid PTI credential found. Deactivating Employee. CardholderGuid: {CardholderGuid}", cardholder.Guid);

                var cardholders = new List<Cardholder> { cardholder };
                var employees = await MapCardholderToInactiveEntity(cardholders).IgnoreCtx();
                var employee = employees.FirstOrDefault();

                if (employee is null)
                {
                    Logger.LogWarning("Employee could not be mapped for deactivation. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    return;
                }

                await EmployeeManager.TryForceDeactivateEmployeeAsync(employee).IgnoreCtx();

                Logger.LogInformation("Employee deactivation requested because no valid PTI credential was found. CardholderGuid: {CardholderGuid}, EmployeeNumber: {EmployeeNumber}", cardholder.Guid, employee.EmployeeNumber);

                return;
            }

            var department = cardholder.GetCustomFields()
     .FirstOrDefault(cf => cf.CustomField.Guid == CF_Department.CustomFieldId)?
     .Value?
     .ToString();

            if (string.IsNullOrWhiteSpace(department))
            {
                Logger.LogWarning("Cardholder Department is empty. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                return;
            }

            credential.IfSome(cred =>
            {
                var employee = CardholderHelper.NewActive(Logger, cred, department);

                employee.IfSome(emp =>
                {
                    Logger.LogInformation("Updating Employee after credential change. CardholderGuid: {CardholderGuid}", cardholder.Guid);
                    _ = EmployeeManager.AddOrUpdateEmployeeAsync(emp).IgnoreCtx();
                });
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing Cardholder credential change.");
        }
    }

    #endregion Credential Events
}