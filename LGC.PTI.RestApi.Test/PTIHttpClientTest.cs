using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.Domain.Common;
using LGC.PTI.HTTPClient;
using LGC.PTI.HTTPClient.Refit.Employees;
using LGC.PTI.Infrastructure;
using LGC.PTI.Refit.Authentication;

namespace LGC.PTI.RestApi.Test;

internal sealed class PTIHttpClientTest
{
    #region Public Fields

    public ILogger Logger = AssemblyInitializer.Logger;
    public IKernel NinjectContainer = AssemblyInitializer.NinjectKernel;

    #endregion Public Fields

    #region Public Methods

    [Test]
    public async Task CreatesANewEmployee_ShouldAddNewEmployee()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();

        var employeeNumber = "9999902";
        var employee = new EmployeeOutDto
        {
            EmployeeNumber = employeeNumber,
            FirstName = "Test",
            LastName = "SS",
            Badge = $"9-{employeeNumber}",
            Language = Language.Français,
            Active = true,
            Department = "1OPERATIONS",
        };
        var result = await client.TryCreateEmployee(employee);

        result.IfFail(ex => Assert.Fail($"Should create employee, but throw error:{ex.Message}"));
        result.IfSucc(emp => emp.IsSuccess.Should().BeTrue());
    }

    [Test]
    public async Task CreatesANewEmployee_ShouldFail_WhenEmployeeExist()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();

        var employee = new EmployeeOutDto
        {
            EmployeeNumber = "9999997",
            FirstName = "Test",
            LastName = "SS",
            Badge = "99-99997",
            Language = Language.Français,
            Active = true,
            Department = "1OPERATIONS",
        };
        var result = await client.TryCreateEmployee(employee);

        result.IfSucc(_ => Assert.Fail("Should not create employee if it is exist"));
        result.IfFail(Logger.Error);
    }

    [Test]
    public async Task ModifiesEmployeeDetails_WhenAttemptingToUpdateEmployyeWithPutRequest()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();

        var employee = new EmployeeOutDto
        {
            EmployeeNumber = "999999",
            FirstName = "Test",
            LastName = "SS",
            Badge = "999-99999",
            Language = Language.Español,
            Department = "1OPERATIONS",
            Active = true,
        };

        var result = await client.TryUpdateEmployee(employee);

        result.IfSucc(status => status.IsSuccess.Should().BeTrue());
        result.IfFail(ex => Assert.Fail($"Should update employee, but throw error:{ex.Message}"));
    }

    [Test]
    public async Task ModifiesEmployeeStatus_WhenAttemptingToDeactivateEmployeeWithPutRequest()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();
        var employee = EmployeeStatusOutDto.Deactivated("999999");
        var result = await client.TryUpdateEmployeeStatus(employee);

        result.IfSucc(status => status.IsSuccess.Should().BeTrue());
        result.IfFail(Logger.Error);
    }

    [Test]
    public async Task ModifiesEmployeeStatus_WhenAttemptingToActivateEmployeeWithPutRequest()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();
        var employee = EmployeeStatusOutDto.Activated("999999");
        var result = await client.TryUpdateEmployeeStatus(employee);

        result.IfSucc(status => status.IsSuccess.Should().BeTrue());
        result.IfFail(Logger.Error);
    }

    [Test]
    public async Task ReceiveAListOfAllEmployees_WhenRequestingGetEmployees()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();
        var employees = await client.TryGetEmployees();
        employees.IfFail(ex => Assert.Fail(ex.Message));
        employees.IfSucc(emps => emps.Should().HaveCountGreaterThan(1));
        employees.IfSucc(emps => Logger.InfoAsJson(emps));
    }

    [Test]
    public async Task ReceiveAToken_WhenCreatingAPTIHttpClient()
    {
        var client = new HttpClientForTesting();
        var response = await client.SendTokenRequest();
        Logger.InfoAsJson(response);
    }

    [Test]
    public async Task ReceiveoneEmployeeinfo_WhenWearchingById()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();
        var employee = await client.TryGetEmployee("999999");

        employee.IfSome(s => Logger.InfoAsJson(s));
        employee.IfFailOrNone(() => Assert.Fail($"employee not found: "));
    }

    [OneTimeSetUp]
    public void SetUp()
    {
        string employeeUrl = "https://api.iqtechportal.com";

        string tokenUrl = "https://account.iqtechportal.com/connect";

        string clientId = "QSL - PTI";
        string clientSecret = "rCrr19";
        var credentials = new AuthenticationCredentials(tokenUrl, clientId, clientSecret);
        var apisettings = new ApiSettings { AuthenticationCredentials = credentials, EmployeesBaseUrl = employeeUrl };
        NinjectContainer.Bind<ApiSettings>().ToConstant(apisettings);
        NinjectContainer.Bind<AuthenticationCredentials>().ToConstant(apisettings.AuthenticationCredentials);
    }

    [Test]
    public async Task UpdatesEmployeePinNumber_WhenAttemptingToSetPinWithPostRequest()
    {
        var client = NinjectContainer.Get<IEmployeeApiSrv>();
        var employee = new FullEmployeeResetPin
        {
            EmployeeNumber = "999999",
            FirstName = "Test",
            LastName = "SS",
            Badge = "999-99999",
            Language = Language.Español,
            Department = "1OPERATIONS",
            Active = true,
            Pin = "12345"
        }; var result = await client.TrySetEmployeePin(employee);

        result.IfSome(s => Logger.InfoAsJson(s));
    }

    //[Test]
    //public async Task ReceiveAToken_WhenUsingRefit()
    //{
    //    var client = RestService.For<IPTIAuthClientApi>("https://account.iqtechportal.com/connect", new RefitSettings(new NewtonsoftJsonContentSerializer(JsonSerializationHelper.DefaultJsonSerializerSettings)));
    //    string clientId = "QSL - PTI";
    //    string clientSecret = "rCrr19";
    //    var encriptedClientDetails = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
    //    var tokenConfig = new TokenConfiguration();
    //    var expectedHeader = "Basic UVNMIC0gUFRJOnJDcnIxOQ==";
    //    var authorizationHeader = $"Basic {encriptedClientDetails}";
    //    authorizationHeader.Should().Be(expectedHeader);
    //    var response = await client.SendTokenRequest(authorizationheader: authorizationHeader, body: tokenConfig);
    //    Logger.InfoAsJson(response);

    //[Test]
    //public async Task ReceiveAToken_WhenUsingAuthenticationService()
    //{
    //    string tokenUrl = "https://account.iqtechportal.com/connect";
    //    string clientId = "QSL - PTI";
    //    string clientSecret = "rCrr19";
    //    var credentials = new AuthenticationCredentials(tokenUrl, clientId, clientSecret);

    //    var authenticationSrv = new AuthenticationSrv(credentials, JsonSerializationHelper.DefaultJsonSerializerSettings);
    //    var tokenStore = new TokenStore(authenticationSrv);
    //    var tokenRslt = await tokenStore.GetTokenAsync();
    //    tokenRslt.IfSucc(token => Logger.InfoAsJson(token));
    //    tokenRslt.IfFail(ex => Logger.InfoAsJson(ex));
    //    tokenRslt.IfFail(ex => Assert.Fail(ex.Message));
    //}

    #endregion Public Methods
}