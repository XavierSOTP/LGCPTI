namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

/// <summary>
/// Represents the employee PIN information sent to the external PTI API.
/// </summary>
public record EmployeePinOutDto
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the employee number associated with the PIN.
    /// </summary>
    /// <remarks>
    /// This property is required by the PTI API. Null and default values are omitted during JSON serialization.
    /// </remarks>
    [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [DefaultValue("")]
    public string EmployeeNumber { get; set; }

    /// <summary>
    /// Gets or sets the PIN assigned to the employee.
    /// </summary>
    /// <remarks>
    /// This property is required by the PTI API. Null and default values are omitted during JSON serialization.
    /// </remarks>
    [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [DefaultValue("")]
    public string Pin { get; set; }

    #endregion Public Properties
}