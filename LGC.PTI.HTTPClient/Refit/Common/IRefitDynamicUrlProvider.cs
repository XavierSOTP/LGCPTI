namespace SoftSolute.Common.Utilities.Refits;

/// <summary>
///
/// </summary>
public interface IRefitDynamicUrlManager
{
    /// <summary>
    /// Placeholder for the dynamic Url, Use this url to set as BaseUrl in the Httpclient defined in Refit configuration
    /// </summary>
    Uri DefaultBaseAddress { get; }

    /// <summary>
    /// Api url that will be dynamic based on the current environment
    /// </summary>
    /// <param name="requestUri"></param>
    /// <returns></returns>
    Option<Uri> TryGetDynamicUrl(Uri requestUri);
}