using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LGC.PTI.Domain;
using LGC.PTI.Domain.Models;
using LGC.PTI.Domain.Mapping;
using Mapster;
using LGC.PTI.GraphQL.Transport;
using LGC.PTI.HTTPClient.GraphQL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LGC.PTI.GraphQL.Transport.Tests
{
    [TestFixture]
    public class GraphQLEmployeeApiSrvTests
    {
        private sealed class FakeClient : IPtiGraphQLClient
        {
            public IReadOnlyList<Employee> Employees { get; set; } = Array.Empty<Employee>();
            public EmployeeInput LastInput { get; private set; }
            public int Reads { get; private set; }
            public int Writes { get; private set; }
            public Func<EmployeeInput, IReadOnlyList<Employee>> OnUpsert { get; set; }

            public Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken ct = default)
            {
                Reads++;
                return Task.FromResult(Employees);
            }

            public Task<IReadOnlyList<Employee>> AddOrUpdateEmployeesAsync(IReadOnlyCollection<EmployeeInput> employees, CancellationToken ct = default)
            {
                Writes++;
                LastInput = employees.Single();
                IReadOnlyList<Employee> result = OnUpsert == null
                    ? new[] { new Employee { Number = LastInput.Number, Pin = LastInput.Pin, FirstName = LastInput.FirstName,
                        LastName = LastInput.LastName, DepartmentNumber = LastInput.DepartmentNumber, Active = LastInput.Active,
                        Deleted = false, Status = "new" } }
                    : OnUpsert(LastInput);
                return Task.FromResult(result);
            }
        }

        private static EmployeeOutDto OutEmployee(string pin = "00123") => new EmployeeOutDto
        {
            EmployeeNumber = "99990001", Pin = pin, Badge = "7281-00123", Language = Language.French,
            FirstName = "Genetec", LastName = "IntegrationTest", Department = "pti", Active = true
        };

        private static Employee ServerEmployee() => new Employee
        {
            Number = "99990001", Pin = "00123", FirstName = "Genetec", LastName = "IntegrationTest",
            DepartmentNumber = "pti", Active = true, Type = "custom", UnrestrictedUsage = true
        };

        [Test]
        public async Task Create_UsesUpsertWithPinAndReturnsNewStatus()
        {
            var fake = new FakeClient();
            var result = await new GraphQLEmployeeApiSrv(fake).TryCreateEmployee(OutEmployee()).RunAsync();
            Assert.True(result.IsSuccess);
            Assert.AreEqual("new", result.OperationStatus);
            Assert.AreEqual("00123", fake.LastInput.Pin);
            Assert.AreEqual("pti", fake.LastInput.DepartmentNumber);
            Assert.AreEqual(1, fake.Writes);
            Assert.AreEqual(0, fake.Reads);
        }

        [Test]
        public async Task Update_DoesNotReadEmployeesFirst()
        {
            var fake = new FakeClient { OnUpsert = e => new[] { new Employee { Number = e.Number, Status = "updated" } } };
            var result = await new GraphQLEmployeeApiSrv(fake).TryUpdateEmployee(OutEmployee()).RunAsync();
            Assert.AreEqual("updated", result.OperationStatus);
            Assert.AreEqual(0, fake.Reads);
            Assert.AreEqual(1, fake.Writes);
        }

        [Test]
        public async Task GetEmployees_MapsNumberDepartmentAndActive()
        {
            var fake = new FakeClient { Employees = new[] { ServerEmployee() } };
            var result = await new GraphQLEmployeeApiSrv(fake).TryGetEmployees().RunAsync();
            Assert.AreEqual("99990001", result.Single().EmployeeNumber);
            Assert.AreEqual("pti", result.Single().Department);
            Assert.True(result.Single().Active);
        }

        [Test]
        public async Task GetEmployee_ByNumberIsCaseInsensitiveAndMissingReturnsEmpty()
        {
            var fake = new FakeClient { Employees = new[] { ServerEmployee() } };
            var service = new GraphQLEmployeeApiSrv(fake);
            Assert.AreEqual("99990001", (await service.TryGetEmployee("99990001").RunAsync()).EmployeeNumber);
            Assert.IsNull((await service.TryGetEmployee("missing").RunAsync()).EmployeeNumber);
            Assert.False((await service.TryGetEmployeeOptional("missing").RunAsync()).IsSome);
        }

        [Test]
        public async Task Deactivate_UsesExistingFullRecordAndPreservesOptionalFlags()
        {
            var fake = new FakeClient { Employees = new[] { ServerEmployee() } };
            var result = await new GraphQLEmployeeApiSrv(fake).TryUpdateEmployeeStatus(EmployeeStatusOutDto.Deactivated("99990001")).RunAsync();
            Assert.True(result.IsSuccess);
            Assert.False(fake.LastInput.Active);
            Assert.AreEqual("00123", fake.LastInput.Pin);
            Assert.AreEqual("Genetec", fake.LastInput.FirstName);
            Assert.AreEqual("pti", fake.LastInput.DepartmentNumber);
            Assert.AreEqual("custom", fake.LastInput.Type);
            Assert.AreEqual(true, fake.LastInput.UnrestrictedUsage);
            Assert.AreEqual(1, fake.Reads);
            Assert.AreEqual(1, fake.Writes);
        }

        [Test]
        public void Deactivate_AmbiguousNumberFailsWithoutWriting()
        {
            var employee = ServerEmployee();
            var fake = new FakeClient { Employees = new[] { employee, ServerEmployee() } };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new GraphQLEmployeeApiSrv(fake).TryUpdateEmployeeStatus(EmployeeStatusOutDto.Deactivated("99990001")).RunAsync());
            Assert.AreEqual(0, fake.Writes);
        }

        [Test]
        public void Deactivate_DeletedOnlyRecordFailsWithoutWriting()
        {
            var employee = ServerEmployee();
            employee.Deleted = true;
            var fake = new FakeClient { Employees = new[] { employee } };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new GraphQLEmployeeApiSrv(fake).TryUpdateEmployeeStatus(EmployeeStatusOutDto.Deactivated("99990001")).RunAsync());
            Assert.AreEqual(0, fake.Writes);
        }

        [Test]
        public void Deactivate_MissingRequiredPinFailsWithoutWriting()
        {
            var employee = ServerEmployee();
            employee.Pin = null;
            var fake = new FakeClient { Employees = new[] { employee } };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new GraphQLEmployeeApiSrv(fake).TryUpdateEmployeeStatus(EmployeeStatusOutDto.Deactivated("99990001")).RunAsync());
            Assert.AreEqual(0, fake.Writes);
        }

        [Test]
        public void Upsert_MissingPinFailsBeforeCallingGraphQL()
        {
            var fake = new FakeClient();
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new GraphQLEmployeeApiSrv(fake).TryCreateEmployee(OutEmployee(null)).RunAsync());
            Assert.AreEqual(0, fake.Writes);
        }

        [Test]
        public void Upsert_WrongResponseNumberFails()
        {
            var fake = new FakeClient { OnUpsert = _ => new[] { new Employee { Number = "another", Status = "updated" } } };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new GraphQLEmployeeApiSrv(fake).TryUpdateEmployee(OutEmployee()).RunAsync());
        }

        [Test]
        public void Upsert_EmptyResponseFails()
        {
            var fake = new FakeClient { OnUpsert = _ => Array.Empty<Employee>() };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new GraphQLEmployeeApiSrv(fake).TryCreateEmployee(OutEmployee()).RunAsync());
        }

        [Test]
        public void RestEmployeeJson_DoesNotContainGraphQLOnlyPin()
        {
            var json = JObject.Parse(JsonConvert.SerializeObject(OutEmployee()));
            Assert.IsNull(json["Pin"]);
            Assert.IsNull(json["pin"]);
            Assert.AreEqual("7281-00123", (string)json["Badge"]);
        }

        [Test]
        public void GenetecCredentialPin_ExposesRawDigitsIncludingLeadingZeroes()
        {
            Assert.AreEqual("00123", CardNumberHelper.TryGetCardNumberText("qsl_00123").IfNone(""));
            Assert.AreEqual("00123", CardNumberHelper.TryGetCardNumberText("qsl-00123").IfNone(""));
            Assert.AreEqual(123, CardNumberHelper.TryGetCardNumber("qsl_00123").IfNone(-1));
        }

        [TestCase("qsl_00123", "00123")]
        [TestCase("qsl-00123", "00123")]
        [TestCase(null, "123")]
        public void EmployeeEntityToEmployeeOutDto_MapsterUsesOriginalPinWhenAvailable(string credentialName, string expectedPin)
        {
            var config = new TypeAdapterConfig();
            new EmployeeModelRegister().Register(config);
            var employee = new EmployeeEntity
            {
                EmployeeNumber = "99990001", Pin = 123, CredName = credentialName,
                FirstName = "Genetec", LastName = "IntegrationTest", Department = "pti",
                Badge = "7281-00123", Active = true
            };

            var mapped = employee.Adapt<EmployeeOutDto>(config);
            Assert.AreEqual(expectedPin, mapped.Pin);
            Assert.AreEqual("99990001", mapped.EmployeeNumber);
            Assert.IsNull(JObject.Parse(JsonConvert.SerializeObject(mapped))["Pin"]);
        }

        [Test]
        public void GenetecCredentialPin_InvalidNameReturnsNone()
        {
            Assert.False(CardNumberHelper.TryGetCardNumberText("qsl-abc").IsSome);
            Assert.False(CardNumberHelper.TryGetCardNumberText(null).IsSome);
        }
    }
}
