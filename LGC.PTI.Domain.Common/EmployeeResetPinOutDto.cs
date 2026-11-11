using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

// properties used to reset employee pin
/// <summary>
/// </summary>
public class EmployeeResetPinOutDto : EmployeeBaseDto
{
    #region Public Properties

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public bool Active { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty(Required = Required.Always)]
    public Language Language { get; set; }

    [JsonProperty(Required = Required.Always)]
    public bool ResetPin { get; set; } = true;

    #endregion Public Properties
}