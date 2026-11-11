namespace LGC.PTI.HTTPClient.Refit.Common;

/// <summary>
/// Defines a contract for resolving dynamic base URLs used by Refit HTTP clients.
/// </summary>
public interface IRefitDynamicUrlManager
{
    /// <summary>
    /// Gets the placeholder base address used when configuring the Refit HTTP client.
    /// </summary>
    /// <remarks>
    /// This address can be used as the initial <c>BaseAddress</c> while the actual
    /// request URL is resolved dynamically at runtime.
    /// </remarks>
    Uri DefaultBaseAddress { get; }

    /// <summary>
    /// Attempts to resolve the dynamic API URL for the specified request URI.
    /// </summary>
    /// <param name="requestUri">
    /// The original request URI that will be used to determine the dynamic API address.
    /// </param>
    /// <returns>
    /// An <see cref="Option{T}"/> containing the resolved URI when a dynamic URL can be determined;
    /// otherwise, <see cref="Option{T}.None"/>.
    /// </returns>
    Option<Uri> TryGetDynamicUrl(Uri requestUri);
}