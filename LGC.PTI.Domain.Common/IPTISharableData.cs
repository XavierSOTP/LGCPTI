using LanguageExt;
using SoftSolute.Common.Utilities.ObjectSS;
using SoftSolute.Net.Refit;
namespace LGC.PTI.Domain.Common;

/// <summary>
/// Provides shared data access across the application, including URL configuration retrieved from the UI.
/// Manages the base URL for employee API endpoints.
/// </summary>
public interface IPTISharableDataProvider : IDynamicUrlProvider
{
    /// <summary>
    /// Sets the base URL for the employee API endpoint.
    /// </summary>
    /// <param name="r">The employee API base URL to set.</param>
    /// <returns>A unit value indicating successful update.</returns>
    Unit SetEmployeeUrl(string r);
}


/// <summary>
/// Implements shared data storage for application-wide configuration including API URLs.
/// Uses thread-safe operations for updating and retrieving the employee API base URL.
/// </summary>
public sealed class PTISharableDataProvider : IPTISharableDataProvider, IDynamicUrlProvider
{
    /// <summary>
    /// The backing field for the stored URL.
    /// </summary>
    private string _url;

    /// <summary>
    /// Gets the currently stored URL, returning an empty string if not set.
    /// </summary>
    public string Url => _url ?? string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PTISharableDataProvider"/> class with an empty URL.
    /// </summary>
    public PTISharableDataProvider()
    {
        _url = string.Empty;
    }

    /// <summary>
    /// Sets the base URL for the employee API endpoint in a thread-safe manner.
    /// </summary>
    /// <param name="r">The employee API base URL to set.</param>
    /// <returns>A unit value indicating successful update.</returns>
    public Unit SetEmployeeUrl(string r)
    {
        ObjectHelper.SafeInterLockExchange(ref _url, r);
        return Unit.Default;
    }
}