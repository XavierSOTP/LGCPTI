namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

public sealed class EmployeeInDto : EmployeeBaseDto
{
    [JsonProperty(
        Required = Required.AllowNull,
        NullValueHandling = NullValueHandling.Include)]
    public new string? EmployeeNumber
    {
        get => base.EmployeeNumber;
        set => base.EmployeeNumber = value;
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public bool Active { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty(Required = Required.Always)]
    public Language Language { get; set; }
}