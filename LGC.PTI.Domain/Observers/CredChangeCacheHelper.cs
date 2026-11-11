using SoftSolute.Common.Utilities.Cache;

namespace LGC.PTI.Domain.Observers;

/// <summary>
/// Provides short-lived cache operations used to prevent duplicate processing
/// of cardholder-related events within a defined backoff period.
/// </summary>
internal static class CredChangeCacheHelper
{
    /// <summary>
    /// Defines the cache domain used to store processed event identifiers.
    /// </summary>
    internal const string PROCESSED_ACS_EVENT_DOMAIN = "ProcessedEvent";

    /// <summary>
    /// Defines how long a processed event identifier remains in the cache.
    /// </summary>
    internal static readonly TimeSpan RETAIN_TIME_IN_MS = TimeSpan.FromMilliseconds(5000);

    /// <summary>
    /// Determines whether the specified identifier currently exists in the backoff cache.
    /// </summary>
    /// <param name="guid">
    /// The unique identifier to look up in the cache.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the identifier exists in the backoff cache;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool ExistInBackOff(Guid guid)
    {
        var key = new CacheKeyId(guid.ToString());
        return MemoryCacheSS.Exist(key, PROCESSED_ACS_EVENT_DOMAIN);
    }

    /// <summary>
    /// Adds the specified identifier to the backoff cache using the configured retention period.
    /// </summary>
    /// <param name="guid">
    /// The unique identifier to add to the cache.
    /// </param>
    public static void AddToBackOff(
      Guid guid)
    {
        var key = new CacheKeyId(guid.ToString());
        MemoryCacheSS.AddItem(
           key,
            PROCESSED_ACS_EVENT_DOMAIN,
            RETAIN_TIME_IN_MS,
            CacheExpiration.Absolute);
    }

    /// <summary>
    /// Determines whether an event associated with the specified identifier should be processed.
    /// </summary>
    /// <param name="guid">
    /// The unique identifier associated with the event.
    /// </param>
    /// <returns>
    /// An <see cref="Option{T}"/> containing <see cref="LanguageExt.Unit"/> when the event
    /// has not been processed recently; otherwise, <see cref="Option{T}.None"/>.
    /// </returns>
    /// <remarks>
    /// When the identifier is not already present in the backoff cache, it is added before
    /// the method indicates that processing should continue.
    /// </remarks>
    internal static Option<LanguageExt.Unit> ShouldProcess(Guid guid)
    {
        var exist = ExistInBackOff(guid);
        if (!exist)
        {
            AddToBackOff(guid);
        }
        return exist ? Option<LanguageExt.Unit>.None : LanguageExt.Unit.Default;
    }
}