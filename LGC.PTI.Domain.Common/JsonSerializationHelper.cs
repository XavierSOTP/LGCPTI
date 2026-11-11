namespace LGC.PTI.Domain.Common;

/// <summary>
/// Provides centralized JSON serialization settings used by the PTI domain.
/// </summary>
public static class JsonSerializationHelper
{
    #region Public Properties

    /// <summary>
    /// Gets the default JSON serializer settings used by the application.
    /// </summary>
    /// <remarks>
    /// The settings omit null and default values, serialize dates using ISO formatting,
    /// and convert property names to camel case.
    /// </remarks>
    public static JsonSerializerSettings DefaultJsonSerializerSettings
    => new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    #endregion Public Properties
}