using Newtonsoft.Json;
using SoftSolute.Common.Utilities;

namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

public sealed class EmployeeStatusOutDto
{
    #region Public Properties

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public bool Active { get; }

    [JsonProperty(Required = Required.Always)]
    public string EmployeeNumber { get; }

    #endregion Public Properties

    #region Public Methods

    public static EmployeeStatusOutDto Activated(NotEmptyString employeeNumber)
    {
        return new EmployeeStatusOutDto(employeeNumber, true);
    }

    public static EmployeeStatusOutDto Deactivated(NotEmptyString employeeNumber)
    {
        return new EmployeeStatusOutDto(employeeNumber, false);
    }

    #endregion Public Methods

    #region Private Constructors

    private EmployeeStatusOutDto(string employeeNumber, bool active)
    {
        EmployeeNumber = employeeNumber;
        Active = active;
    }

    #endregion Private Constructors
}