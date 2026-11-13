namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

/// <summary>
/// Represents the employee data sent to the external PTI API.
/// </summary>
/// <remarks>
/// Extends <see cref="EmployeeBaseDto"/> with the employee activation state
/// and language required by the PTI API.
/// </remarks>
public sealed class EmployeeOutDto : EmployeeBaseDto
{
    #region Public Properties

    /// <summary>PIN sent to GraphQL only. Never serialize into the legacy REST employee body.</summary>
    [JsonIgnore]
    public string Pin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the employee is active.
    /// </summary>
    /// <remarks>
    /// The value is included during JSON serialization even when it matches its default value.
    /// The default value is <see langword="true"/>.
    /// </remarks>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets the employee language.
    /// </summary>
    /// <remarks>
    /// This property is required by the PTI API and is always included during JSON serialization.
    /// </remarks>
    [JsonProperty(Required = Required.Always, DefaultValueHandling = DefaultValueHandling.Include)]
    public Language Language { get; set; }

    #endregion Public Properties
}