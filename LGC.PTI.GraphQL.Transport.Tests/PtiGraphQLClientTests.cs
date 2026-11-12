using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LGC.PTI.GraphQL.Transport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LGC.PTI.GraphQL.Transport.Tests
{
    [TestFixture]
    public class PtiGraphQLClientTests
    {
        private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1800000000);

        private static string Token(long expiry)
        {
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"exp\":" + expiry + "}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return header + "." + payload + ".signature";
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private static string LoginResult(string jwt) => "{\"data\":{\"login\":{\"token\":\"" + jwt + "\"}}}";

        private sealed class FakeHandler : HttpMessageHandler
        {
            public int Logins;
            public int Operations;
            public string LastAuthorization;
            public JObject LastPayload;
            public Func<int, HttpResponseMessage> OperationResponse = _ => Json("{\"data\":{\"getEmployees\":[]}}");
            public Func<int, HttpResponseMessage> LoginResponse = _ => Json(LoginResult(Token(Now.ToUnixTimeSeconds() + 3600)));

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var data = JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                if (((string)data["query"]).Contains("mutation Login"))
                {
                    var count = Interlocked.Increment(ref Logins);
                    return Task.FromResult(LoginResponse(count));
                }
                LastAuthorization = request.Headers.TryGetValues("Authorization", out var values) ? values.Single() : null;
                LastPayload = data;
                var attempt = Interlocked.Increment(ref Operations);
                return Task.FromResult(OperationResponse(attempt));
            }
        }

        private static PtiGraphQLClient Client(FakeHandler fake, Func<DateTimeOffset> now = null) =>
            new PtiGraphQLClient(new HttpClient(fake) { BaseAddress = new Uri("https://example.test/graphql") }, "test@example.test", "password", now ?? (() => Now));

        private static EmployeeInput EmployeeInput() => new EmployeeInput
        {
            Number = "99990001", Pin = "00123", FirstName = "Genetec",
            LastName = "IntegrationTest", DepartmentNumber = "pti", Active = true
        };

        [Test]
        public async Task GetEmployees_SendsRawJwtWithoutBearer_AndReusesLogin()
        {
            var fake = new FakeHandler();
            var client = Client(fake);
            await client.GetEmployeesAsync();
            await client.GetEmployeesAsync();
            Assert.AreEqual(1, fake.Logins);
            Assert.AreEqual(2, fake.Operations);
            Assert.AreEqual(Token(Now.ToUnixTimeSeconds() + 3600), fake.LastAuthorization);
        }

        [Test]
        public async Task Upsert_PreservesPinLeadingZeroes_AndDoesNotSendBadgeOrLanguage()
        {
            var fake = new FakeHandler
            {
                OperationResponse = _ => Json("{\"data\":{\"addOrUpdateEmployees\":[{\"number\":\"99990001\",\"pin\":\"00123\",\"active\":true,\"deleted\":false,\"status\":\"new\",\"error\":null}]}}")
            };
            var result = await Client(fake).AddOrUpdateEmployeesAsync(new[] { EmployeeInput() });
            Assert.AreEqual("00123", (string)fake.LastPayload["variables"]["employees"][0]["pin"]);
            Assert.IsNull(fake.LastPayload["variables"]["employees"][0]["badge"]);
            Assert.IsNull(fake.LastPayload["variables"]["employees"][0]["language"]);
            Assert.AreEqual("new", result[0].Status);
            Assert.AreEqual("00123", result[0].Pin);
        }

        [Test]
        public async Task Upsert_Deactivation_UsesActiveFalseAndNotDeleteMutation()
        {
            var fake = new FakeHandler
            {
                OperationResponse = _ => Json("{\"data\":{\"addOrUpdateEmployees\":[{\"number\":\"99990001\",\"active\":false,\"deleted\":false,\"status\":\"updated\",\"error\":null}]}}")
            };
            var employee = EmployeeInput();
            employee.Active = false;
            var result = await Client(fake).AddOrUpdateEmployeesAsync(new[] { employee });
            Assert.False((bool)fake.LastPayload["variables"]["employees"][0]["active"]);
            StringAssert.DoesNotContain("deleteEmployees", (string)fake.LastPayload["query"]);
            Assert.False(result[0].Active);
        }

        [Test]
        public void Upsert_RejectsMissingMandatoryPinBeforeCallingApi()
        {
            var fake = new FakeHandler();
            var employee = EmployeeInput();
            employee.Pin = null;
            Assert.ThrowsAsync<ArgumentException>(async () => await Client(fake).AddOrUpdateEmployeesAsync(new[] { employee }));
            Assert.AreEqual(0, fake.Operations);
            Assert.AreEqual(0, fake.Logins);
        }

        [Test]
        public void QueryValidationError_ThrowsGraphQLRequestException()
        {
            var fake = new FakeHandler { OperationResponse = _ => Json("{\"errors\":[{\"message\":\"Cannot query field badge\"}]}") };
            var ex = Assert.ThrowsAsync<GraphQLRequestException>(async () => await Client(fake).GetEmployeesAsync());
            StringAssert.Contains("Cannot query field", ex.Message);
            Assert.AreEqual(1, fake.Operations);
        }

        [Test]
        public void EmployeeBusinessError_FailsEntireMutation()
        {
            var fake = new FakeHandler
            {
                OperationResponse = _ => Json("{\"data\":{\"addOrUpdateEmployees\":[{\"number\":\"99990001\",\"status\":\"updated\",\"error\":\"Rejected\"}]}}")
            };
            Assert.ThrowsAsync<GraphQLRequestException>(async () => await Client(fake).AddOrUpdateEmployeesAsync(new[] { EmployeeInput() }));
        }

        [Test]
        public async Task InvalidAuthenticationToken_ReloginsAndRetriesOnce()
        {
            var fake = new FakeHandler { OperationResponse = i => i == 1
                ? Json("{\"errors\":[{\"message\":\"Invalid authentication token\"}],\"data\":null}", HttpStatusCode.Unauthorized)
                : Json("{\"data\":{\"getEmployees\":[]}}") };
            await Client(fake).GetEmployeesAsync();
            Assert.AreEqual(2, fake.Logins);
            Assert.AreEqual(2, fake.Operations);
        }

        [Test]
        public void RepeatedInvalidAuthenticationToken_DoesNotLoopForever()
        {
            var fake = new FakeHandler { OperationResponse = _ =>
                Json("{\"errors\":[{\"message\":\"Invalid authentication token\"}],\"data\":null}", HttpStatusCode.Unauthorized) };
            Assert.ThrowsAsync<GraphQLRequestException>(async () => await Client(fake).GetEmployeesAsync());
            Assert.AreEqual(2, fake.Logins);
            Assert.AreEqual(2, fake.Operations);
        }

        [Test]
        public async Task NearExpiry_ReloginsProactively()
        {
            var now = Now;
            var fake = new FakeHandler { LoginResponse = i => Json(LoginResult(Token(Now.ToUnixTimeSeconds() + (i == 1 ? 3600 : 7200)))) };
            var client = Client(fake, () => now);
            await client.GetEmployeesAsync();
            now = now.AddMinutes(56);
            await client.GetEmployeesAsync();
            Assert.AreEqual(2, fake.Logins);
        }

        [Test]
        public async Task ConcurrentRequests_UseSingleLogin()
        {
            var fake = new FakeHandler();
            var client = Client(fake);
            await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.GetEmployeesAsync()));
            Assert.AreEqual(1, fake.Logins);
            Assert.AreEqual(10, fake.Operations);
        }

        [Test]
        public void MissingGraphQLData_ThrowsInsteadOfReturningEmptyCollection()
        {
            var fake = new FakeHandler { OperationResponse = _ => Json("{\"data\":null}") };
            Assert.ThrowsAsync<GraphQLRequestException>(async () => await Client(fake).GetEmployeesAsync());
        }

        [Test]
        public void MalformedJson_ThrowsSanitizedError()
        {
            var fake = new FakeHandler { OperationResponse = _ => Json("not json") };
            var ex = Assert.ThrowsAsync<GraphQLRequestException>(async () => await Client(fake).GetEmployeesAsync());
            StringAssert.Contains("invalid JSON", ex.Message);
        }

        [Test]
        public void HttpServerError_ThrowsGraphQLRequestException()
        {
            var fake = new FakeHandler { OperationResponse = _ => Json("{}", HttpStatusCode.InternalServerError) };
            var ex = Assert.ThrowsAsync<GraphQLRequestException>(async () => await Client(fake).GetEmployeesAsync());
            Assert.AreEqual(HttpStatusCode.InternalServerError, ex.StatusCode);
        }
    }
}
