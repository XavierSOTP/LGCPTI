using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.Domain.Common;
using LGC.PTI.Shared.Common;

namespace LGC.PTI.RestApi.Test;

public sealed class DesirializingDtosTests
{
    #region Public Fields

    public ILogger Logger = AssemblyInitializer.Logger;

    #endregion Public Fields

    #region Public Methods

    [Test]
    public void CreatesAnEmployee_WhenSerializingEmployeeOutDto()
    {
        PTITodoCode.Instance.PendingRefactoring(Logger, "Must change name");
        var expected = new EmployeeOutDto
        {
            EmployeeNumber = "9999993",
            FirstName = "Test",
            LastName = "SS",
            Badge = "99-99993",
            Language = Language.Français,
            Active = true,
            Department = "1OPERATIONS",
        };

        var jsonString = JsonConvert.SerializeObject(expected, Formatting.Indented, JsonSerializationHelper.DefaultJsonSerializerSettings);
        Logger.Info(jsonString);
    }

    [Test]
    public async Task GetListOfEmployees_WhenDeserializingEmployeeInDto()
    {
        var expected = new EmployeeInDto
        {
            EmployeeNumber = "177"
        };

        var path = ProjectResourcesHelper.GetResourcePath("GetEmployeesInDtos.json");
        var rsltDtos = await DtoProvider.TryGetDtoAsync<IEnumerable<EmployeeInDto>>(path);
        rsltDtos.IfSucc(ds => Logger.InfoAsJson(ds));
        rsltDtos.IfFail(ex => Logger.Error(ex));
        rsltDtos.IfFail(ex => Assert.Fail());
        var dtos = rsltDtos.IfFail(Enumerable.Empty<EmployeeInDto>());
        Logger.InfoAsJson(dtos);
        var actual = dtos.First();

        actual.EmployeeNumber.Should().Be(expected.EmployeeNumber);
    }

    [Test]
    public void ResetsPin_WhenSerializingEmployeePinStatus()
    {
        var expected = new EmployeeResetPinOutDto()
        {
            FirstName = "Test",
            LastName = "SS",
            Badge = "99-99993",
            Language = Language.Français,
            Active = true,
            Department = "1OPERATIONS",
            ResetPin = false
        };

        var jsonString = JsonConvert.SerializeObject(expected, Formatting.Indented, JsonSerializationHelper.DefaultJsonSerializerSettings);
        Logger.Info(jsonString);
    }

    [Test]
    public void SetAPinToAnEmployee_WhenSerializingEmployeePinOutDto()
    {
        var expected = new EmployeePinOutDto
        {
            Pin = "12345",
            EmployeeNumber = "9999993",
        };

        var jsonString = JsonConvert.SerializeObject(expected, Formatting.Indented, JsonSerializationHelper.DefaultJsonSerializerSettings);
        Logger.Info(jsonString);
        var actual = JsonConvert.DeserializeObject<EmployeePinOutDto>(jsonString);
        actual.Pin.Should().Be(expected.Pin);
        actual.EmployeeNumber.Should().Be(expected.EmployeeNumber);
    }

    [Test]
    public void UpdatesEmployeeByNumber_WhenSerializingEmployee()
    {
        var expected = new EmployeeOutDto
        {
            FirstName = "Test",
            LastName = "SS",
            Badge = "999-99999",
            Language = Language.Español,
            Department = "1OPERATIONS",
            Active = true,
        };

        var jsonString = JsonConvert.SerializeObject(expected, Formatting.Indented, JsonSerializationHelper.DefaultJsonSerializerSettings);
        Logger.Info(jsonString);
    }

    [Test]
    public void UpdatesStatus_WhenSerializingEmployeeStatus()
    {
        var expectedActivate = EmployeeStatusOutDto.Activated("999999");
        var expectedDeactivate = EmployeeStatusOutDto.Deactivated("999999");

        var jsonString = JsonConvert.SerializeObject(expectedActivate, Formatting.Indented, JsonSerializationHelper.DefaultJsonSerializerSettings);
        jsonString += "\n" + JsonConvert.SerializeObject(expectedDeactivate, Formatting.Indented, JsonSerializationHelper.DefaultJsonSerializerSettings);
        Logger.Info(jsonString);
    }

    #endregion Public Methods
}