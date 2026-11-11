using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using R3;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.ReflectionSS;
using SoftSolute.Plugin.Exe;
using SoftSolute.Plugin.Exe.EntityObservers;
using SoftSolute.Utiities.Plugin.Adapters;
using Unit = LanguageExt.Unit;

namespace LGC.PTI.Domain;

/// <summary>
/// Notifies subscribers when the application has completed initialization.
/// </summary>
/// <remarks>
/// Provides an event-based mechanism that allows dependent components to execute
/// asynchronous post-initialization operations after the application is ready.
/// </remarks>
public interface IAppInitializedNotifierPlugin
{
    /// <summary>
    /// Occurs when application initialization has completed.
    /// </summary>
    /// <remarks>
    /// Subscribers can register asynchronous handlers that receive the initialization
    /// cancellation token.
    /// </remarks>
    event Func<CancellationToken, Task> Initialized;

    /// <summary>
    /// Notifies all registered subscribers that application initialization has completed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the notification operation.
    /// </param>
    /// <returns>
    /// A task that returns a <see cref="LanguageExt.Unit"/> value after all registered
    /// handlers have completed.
    /// </returns>
    Task<LanguageExt.Unit> NotifyAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides the default implementation of <see cref="IAppInitializedNotifierPlugin"/>.
/// </summary>
/// <remarks>
/// Registered initialization handlers are executed sequentially in the order in which
/// they appear in the event invocation list.
/// </remarks>
public sealed class AppInitializedNotifierPlugin : IAppInitializedNotifierPlugin
{
    /// <summary>
    /// Occurs when application initialization has completed.
    /// </summary>
    public event Func<CancellationToken, Task> Initialized;

    /// <summary>
    /// Notifies all registered initialization subscribers.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel notification before or between handler executions.
    /// </param>
    /// <returns>
    /// A task that returns a <see cref="LanguageExt.Unit"/> value when all registered
    /// handlers have completed.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is cancelled before a handler executes.
    /// </exception>
    public async Task<LanguageExt.Unit> NotifyAsync(
        CancellationToken cancellationToken = default)
    {
        var handlers = Initialized?.GetInvocationList();

        if (handlers == null)
        {
            return Prelude.unit;
        }

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ((Func<CancellationToken, Task>)handler)(
                cancellationToken)
                .ConfigureAwait(false);
        }

        return Prelude.unit;
    }
}

/// <summary>
/// Provides a reusable base implementation for SDK-based plugin applications.
/// </summary>
/// <typeparam name="TSelf">
/// The concrete application type deriving from <see cref="SdkApplicationBasePlugin{TSelf}"/>.
/// </typeparam>
/// <remarks>
/// The base class coordinates application initialization, entity-observer initialization,
/// initialization notifications, and application lifecycle callbacks.
/// </remarks>
public abstract class SdkApplicationBasePlugin<TSelf> where TSelf : SdkApplicationBasePlugin<TSelf>
{
    private Aff<Unit> _initializedState;

    /// <summary>
    /// Gets the unique entity observers managed internally by the application.
    /// </summary>
    private IEnumerable<IEntityObserver> InternalEntityObservers { get; }

    /// <summary>
    /// Gets a read-only collection of the entity observers managed by the application.
    /// </summary>
    public IReadOnlyCollection<IEntityObserver> EntityObservers => InternalEntityObservers.ToList().AsReadOnly();

    /// <summary>
    /// Gets the notifier used to signal that application initialization has completed.
    /// </summary>
    public IAppInitializedNotifierPlugin AppInitializedNotifier { get; }

    /// <summary>
    /// Gets the entity observers supplied directly to the application.
    /// </summary>
    public IEnumerable<IEntityObserver> EntityObservers1 { get; }

    /// <summary>
    /// Gets or sets the application-specific initialization callback.
    /// </summary>
    /// <remarks>
    /// The default implementation performs no operation and returns a successful unit effect.
    /// </remarks>
    public Func<CancellationToken, Aff<Unit>> HandleInitialization { get; set; } = (CancellationToken cts) => Prelude.unitAff;

    /// <summary>
    /// Gets or sets the application-specific start callback.
    /// </summary>
    /// <remarks>
    /// The default implementation performs no operation and returns a successful unit effect.
    /// </remarks>
    public Func<CancellationToken, Aff<Unit>> HandleStart { get; set; } = (CancellationToken cts) => Prelude.unitAff;

    /// <summary>
    /// Gets or sets the application-specific stop callback.
    /// </summary>
    /// <remarks>
    /// The default implementation performs no operation and returns a successful unit effect.
    /// </remarks>
    public Func<CancellationToken, Aff<Unit>> HandleStop { get; set; } = (CancellationToken cts) => Prelude.unitAff;

    /// <summary>
    /// Gets or sets the logger used for application lifecycle and initialization messages.
    /// </summary>
    public Microsoft.Extensions.Logging.ILogger Logger { get; set; }

    /// <summary>
    /// Gets or sets the binding flags used when discovering entity observers through reflection.
    /// </summary>
    private BindingFlags ObserverAttBindingFlag { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SdkApplicationBasePlugin{TSelf}"/> class
    /// using an explicitly supplied collection of entity observers.
    /// </summary>
    /// <param name="logger">
    /// The logger used for application lifecycle and initialization messages.
    /// </param>
    /// <param name="bindingFlags">
    /// The reflection binding flags used when working with observer properties.
    /// </param>
    /// <param name="appInitializedNotifier">
    /// The notifier used to signal application initialization completion.
    /// </param>
    /// <param name="entityObservers">
    /// The entity observers managed by the application.
    /// </param>
    /// <remarks>
    /// Duplicate observers are removed based on their <see cref="IEntityObserver.Id"/> value.
    /// </remarks>
    protected SdkApplicationBasePlugin(Microsoft.Extensions.Logging.ILogger logger, BindingFlags bindingFlags, IAppInitializedNotifierPlugin appInitializedNotifier, IEnumerable<IEntityObserver> entityObservers)
    {
        Logger = logger;
        ObserverAttBindingFlag = bindingFlags;
        AppInitializedNotifier = appInitializedNotifier;
        EntityObservers1 = entityObservers;
        _initializedState = Prelude.FailAff<Unit>(Error.New("SdkApplication was not Initialized")).OnFail(delegate (Exception ex)
        {
            Logger.LogError(ex, null);
        });
        GenericComparer<IEntityObserver> equalityComparer = new GenericComparer<IEntityObserver>((IEntityObserver x) => x.Id);
        InternalEntityObservers = entityObservers.AsUniqueSeq(equalityComparer, "entityObservers").Value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SdkApplicationBasePlugin{TSelf}"/> class
    /// and discovers entity observers from attributed properties.
    /// </summary>
    /// <param name="logger">
    /// The logger used for application lifecycle and initialization messages.
    /// </param>
    /// <param name="bindingFlags">
    /// The reflection binding flags used when discovering observer properties.
    /// </param>
    /// <param name="appInitializedNotifier">
    /// The notifier used to signal application initialization completion.
    /// </param>
    /// <remarks>
    /// Observer properties marked with <see cref="EntityObserverAttribute"/> are discovered
    /// through reflection and duplicate observers are removed based on
    /// <see cref="IEntityObserver.Id"/>.
    /// </remarks>
    protected SdkApplicationBasePlugin(Microsoft.Extensions.Logging.ILogger logger, BindingFlags bindingFlags, IAppInitializedNotifierPlugin appInitializedNotifier)
    {
        Logger = logger;
        ObserverAttBindingFlag = bindingFlags;
        AppInitializedNotifier = appInitializedNotifier;
        _initializedState = Prelude.FailAff<Unit>(Error.New("SdkApplication was not Initialized")).OnFail(delegate (Exception ex)
        {
            Logger.LogError(ex, null);
        });
        List<IEntityObserver> value = GetEntityObservers().ToList();
        GenericComparer<IEntityObserver> equalityComparer = new GenericComparer<IEntityObserver>((IEntityObserver x) => x.Id);
        InternalEntityObservers = value.AsUniqueSeq(equalityComparer, "entityObservers").Value;
        IReadOnlyCollection<IEntityObserver> GetEntityObservers()
        {
            return (from p in ReflectionHelper.GetPropertiesWithAttribute<TSelf, EntityObserverAttribute>(ObserverAttBindingFlag)
                    select (IEntityObserver)p.GetValue(this)).ToList();
        }
    }

    /// <summary>
    /// Initializes the application and its registered entity observers.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the initialization operation.
    /// </param>
    /// <returns>
    /// A memoized asynchronous effect representing application initialization.
    /// </returns>
    /// <remarks>
    /// Initialization executes the application-specific initialization callback,
    /// initializes all entity observers, marks the application as initialized,
    /// and notifies initialization subscribers.
    /// </remarks>
    public Aff<Unit> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        return (from _2 in HandleInitialization(cancellationToken)
                from _3 in InitializeObservers(cancellationToken).OnSucc((Action)delegate
                {
                    _initializedState = Prelude.unitAff;
                })
                from _4 in NotifyAppInitialized(cancellationToken)
                select Prelude.unit).Memo();
    }

    /// <summary>
    /// Initializes all entity observers managed by the application.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel observer initialization.
    /// </param>
    /// <returns>
    /// A memoized asynchronous effect representing initialization of all observers.
    /// </returns>
    /// <remarks>
    /// Observer initialization tasks are collected and executed together.
    /// </remarks>
    protected virtual Aff<Unit> InitializeObservers(CancellationToken cancellationToken = default(CancellationToken))
    {
        return LanguageExtAffExtensions.Aff<Unit>(async delegate
        {
            await InternalEntityObservers.Aggregate(new List<Task>(), delegate (List<Task> list, IEntityObserver obs)
            {
                list.Add(obs.InitializeAsync(obs, ObserverAttBindingFlag));
                return list;
            }).ToArray().WhenAll(cancellationToken)
                .IgnoreCtx();
            return Prelude.unit;
        }).Memo();
    }

    /// <summary>
    /// Notifies subscribers that application and observer initialization has completed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token used to cancel the notification operation before notification begins.
    /// </param>
    /// <returns>
    /// An asynchronous effect representing the notification operation.
    /// </returns>
    private Aff<Unit> NotifyAppInitialized(CancellationToken cancellationToken = default(CancellationToken))
    {
        return LanguageExtAffExtensions.Aff<Unit>(async delegate
        {
            cancellationToken.ThrowIfCancellationRequested();
            await AppInitializedNotifier.NotifyAsync().IgnoreCtx();
            return Prelude.unit;
        });
    }
}