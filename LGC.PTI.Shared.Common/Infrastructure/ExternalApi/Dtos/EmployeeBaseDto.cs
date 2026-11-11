namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

/// <summary>
/// Defines the common employee information shared by employee-related DTOs.
/// </summary>
public abstract class EmployeeBaseDto
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the employee badge identifier.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public string Badge { get; set; }

    /// <summary>
    /// Gets or sets the department associated with the employee.
    /// </summary>
    [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string Department { get; set; }

    /// <summary>
    /// Gets or sets the employee number.
    /// </summary>
    /// <remarks>
    /// This property must contain an integer value.
    /// </remarks>
    [JsonProperty(Required = Required.Always)]
    public string EmployeeNumber { get; set; }

    /// <summary>
    /// Gets or sets the employee's first name.
    /// </summary>
    [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [DefaultValue("")]
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the employee's last name.
    /// </summary>
    [JsonProperty(Required = Required.Always, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [DefaultValue("")]
    public string LastName { get; set; }

    #endregion Public Properties
}