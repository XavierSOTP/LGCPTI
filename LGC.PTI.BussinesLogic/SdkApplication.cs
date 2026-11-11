using Genetec.Sdk;
using LanguageExt;
using LGC.PIT.Plugin.Common.Configs;
using LGC.PTI.Domain;
using LGC.PTI.Domain.Common;
using LGC.PTI.Domain.Observers;
using Microsoft.Extensions.Logging;
using SoftSolute.Plugin.Common.Helpers;
using SoftSolute.Plugin.Exe;
using SoftSolute.Plugin.Exe.EntityObservers;

namespace LGC.PTI.BussinesLogic;

/// <summary>
/// Represents the main SDK application instance for the LGC PTI plugin.
/// Manages entity observers for credentials and cardholders within the Genetec Security Center environment.
/// Serves as a bridge between the Genetec SDK and the plugin's domain services.
/// </summary>
internal class SdkApplication : SdkApplicationBasePlugin<SdkApplication>
{
    /// <summary>
    /// The observer responsible for monitoring and handling credential changes within the system.
    /// </summary>
    [EntityObserver]
    public CredentialObserver CredentialObserver { get; private set; }

    /// <summary>
    /// The credential store for managing authentication-related credential storage and retrieval.
    /// </summary>
    public IAuthCredentialStore AuthCredentialStore { get; }

    /// <summary>
    /// The application settings configuration accessible throughout the SDK application lifetime.
    /// </summary>
    public IConfigurableAppSettings Settings { get; }

    /// <summary>
    /// The observer responsible for monitoring and handling cardholder entity changes within the system.
    /// </summary>
    [EntityObserver]
    public CardholderObserver CardholderObserver { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SdkApplication"/> class.
    /// Registers entity observers, initializes lifecycle handlers, and sets up credential and settings management.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic messages.</param>
    /// <param name="credentialObserver">The credential observer for monitoring credential changes.</param>
    /// <param name="appInitializedNotifier">The notification service for application initialization events.</param>
    /// <param name="authCredentialStore">The credential storage service for authentication credentials.</param>
    /// <param name="settings">The application settings configuration.</param>
    /// <param name="cardholderObserver">The cardholder observer for monitoring cardholder entity changes.</param>
    public SdkApplication(
      ILogger<SdkApplication> logger,
      CredentialObserver credentialObserver,
      IAppInitializedNotifierPlugin appInitializedNotifier,
      IAuthCredentialStore authCredentialStore,
      IConfigurableAppSettings settings,
      CardholderObserver cardholderObserver)
      : base(
          logger,
          ReflectionHelper.PublicInstance,
          appInitializedNotifier,
          new IEntityObserver[]
          {
            credentialObserver,
            cardholderObserver
          })
    {
        HandleInitialization = _ =>
            SuccessAff(Prelude.unit);

        HandleStart = HandleStartAction;

        HandleStop = _ =>
            SuccessAff(Prelude.unit);

        CredentialObserver = credentialObserver;
        AuthCredentialStore = authCredentialStore;
        Settings = settings;
        CardholderObserver = cardholderObserver;

        Logger.LogInformation(
            "Observers detected: {Observers}",
            string.Join(
                ", ",
                EntityObservers.Select(x => x.GetType().Name)));
    }

    /// <summary>
    /// Gets the Genetec SDK engine instance for this application.
    /// </summary>
    public IEngine Engine { get; }

    /// <summary>
    /// Handles the application start event.
    /// Performs any necessary initialization when the SDK application starts.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to signal shutdown requests.</param>
    /// <returns>An effect that completes successfully on application start.</returns>
    private Aff<Unit> HandleStartAction(CancellationToken cancellationToken)
    {
        return SuccessAff(Prelude.unit);
    }

    /// <summary>
    /// Creates a successful asynchronous effect with the specified value.
    /// Wraps the value in a LanguageExt Aff monad for functional composition.
    /// </summary>
    /// <typeparam name="A">The type of the value to wrap.</typeparam>
    /// <param name="value">The value to wrap in a successful effect.</param>
    /// <returns>An Aff monad representing a successful computation with the given value.</returns>
    [Pure]
    public static Aff<A> SuccessAff<A>(A value)
    {
        return LanguageExt.Aff<A>.Success(value);
    }
}


