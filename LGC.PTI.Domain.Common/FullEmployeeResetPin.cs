using Newtonsoft.Json;

namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

/// <summary>
/// </summary>
public class FullEmployeeResetPin : EmployeeResetPinOutDto
{
    #region Public Properties

    [JsonProperty(Required = Required.Always)]
    public string Pin { get; set; }

    #endregion Public Properties
}