using R3;
using System.Threading;

namespace SoftSolute.Utiities.Plugin.Adapters;

/// <summary>
/// Provides helper methods for creating and executing LanguageExt asynchronous effects.
/// </summary>
public static class LanguageExtAffExtensions
{
    /// <summary>
    /// Creates a LanguageExt asynchronous effect from the specified asynchronous operation.
    /// </summary>
    /// <typeparam name="T">
    /// The type of value produced by the operation.
    /// </typeparam>
    /// <param name="operation">
    /// The asynchronous operation to wrap.
    /// </param>
    /// <returns>
    /// An <see cref="Aff{T}"/> representing the specified operation.
    /// </returns>
    public static Aff<T> Aff<T>(Func<Task<T>> operation)
    {
        return Prelude.Aff(() => new ValueTask<T>(operation()));
    }

    /// <summary>
    /// Executes the specified asynchronous effect and returns its result as a task.
    /// </summary>
    /// <typeparam name="T">
    /// The type of value produced by the effect.
    /// </typeparam>
    /// <param name="effect">
    /// The asynchronous effect to execute.
    /// </param>
    /// <returns>
    /// A task containing the resulting <see cref="Fin{T}"/>.
    /// </returns>
    public static Task<Fin<T>> RunAsync<T>(this Aff<T> effect)
    {
        return effect.Run().AsTask();
    }

    /// <summary>
    /// Executes the specified asynchronous effect synchronously.
    /// </summary>
    /// <typeparam name="T">
    /// The type of value produced by the effect.
    /// </typeparam>
    /// <param name="effect">
    /// The asynchronous effect to execute.
    /// </param>
    /// <returns>
    /// The resulting <see cref="Fin{T}"/>.
    /// </returns>
    public static Fin<T> RunSync<T>(this Aff<T> effect)
    {
        return effect.Run().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the specified asynchronous effect and converts any thrown exception
    /// into a failed <see cref="Fin{T}"/> result.
    /// </summary>
    /// <typeparam name="A">
    /// The type of value produced by the effect.
    /// </typeparam>
    /// <param name="source">
    /// The asynchronous effect to execute safely.
    /// </param>
    /// <returns>
    /// A task containing either the successful result or a failure created from
    /// the captured exception.
    /// </returns>
    public static async Task<Fin<A>> RunSafelyAsync<A>(this Aff<A> source)
    {
        try
        {
            return await LanguageExtAffExtensions
                .RunAsync(source)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return Prelude.FinFail<A>(Error.New(exception));
        }
    }
}

/// <summary>
/// Provides adapter methods for working with R3 observables and asynchronous subscribers.
/// </summary>
public static class R3Adapter
{
    /// <summary>
    /// Subscribes an asynchronous handler to the specified observable sequence.
    /// </summary>
    /// <typeparam name="T">
    /// The type of values produced by the observable.
    /// </typeparam>
    /// <param name="source">
    /// The observable sequence to subscribe to.
    /// </param>
    /// <param name="onNextAsync">
    /// The asynchronous handler invoked for each emitted value.
    /// </param>
    /// <param name="awaitOperation">
    /// Defines how asynchronous observer operations are coordinated.
    /// </param>
    /// <returns>
    /// An <see cref="IDisposable"/> that can be used to unsubscribe from the observable.
    /// </returns>
    public static IDisposable SubscribeTask<T>(
     this Observable<T> source,
     Func<T, CancellationToken, Task> onNextAsync,
     AwaitOperation awaitOperation = AwaitOperation.Sequential)
    {
        return source.SubscribeAwait(
            (value, cancellationToken) =>
                new ValueTask(
                    onNextAsync(
                        value,
                        cancellationToken)),
            awaitOperation);
    }
}