using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LGC.PTI.EmployeeSync.Application;
using LGC.PTI.EmployeeSync.Domain;
using NUnit.Framework;

namespace LGC.PTI.GraphQL.Transport.Tests
{
    [TestFixture]
    public class EmployeeSynchronizationDomainTests
    {
        private static PtiEmployee Employee() =>
            new PtiEmployee(new EmployeeNumber("99990001"), new EmployeePin("00123"),
                "Genetec", "IntegrationTest", "pti", true, "staff", true);

        private static PtiEmployeeSnapshot Snapshot(string number = "99990001", bool deleted = false, string pin = "00123") =>
            new PtiEmployeeSnapshot(number, pin, "Genetec", "IntegrationTest", "pti",
                true, deleted, "staff", true);

        private sealed class FakeGateway : IPtiEmployeeGateway
        {
            public IReadOnlyList<PtiEmployeeSnapshot> RemoteEmployees { get; set; } = Array.Empty<PtiEmployeeSnapshot>();
            public PtiEmployee SavedEmployee { get; private set; }
            public int Reads { get; private set; }
            public int Writes { get; private set; }

            public Task<IReadOnlyList<PtiEmployeeSnapshot>> GetEmployeesAsync(CancellationToken cancellationToken = default)
            {
                Reads++;
                return Task.FromResult(RemoteEmployees);
            }

            public Task<EmployeeSyncOutcome> SaveAsync(PtiEmployee employee, CancellationToken cancellationToken = default)
            {
                SavedEmployee = employee;
                Writes++;
                var result = new PtiEmployeeSnapshot(
                    employee.Number.Value, employee.Pin.Value, employee.FirstName,
                    employee.LastName, employee.DepartmentNumber, employee.Active,
                    false, employee.Type, employee.UnrestrictedUsage);
                return Task.FromResult(new EmployeeSyncOutcome(employee.Number, EmployeeChangeKind.Updated, result));
            }
        }

        [Test]
        public void EmployeeNumber_IsValueObjectWithCaseInsensitiveEquality()
        {
            Assert.AreEqual(new EmployeeNumber("abc001"), new EmployeeNumber("ABC001"));
            Assert.AreEqual(new EmployeeNumber("abc001").GetHashCode(), new EmployeeNumber("ABC001").GetHashCode());
            Assert.AreNotEqual(new EmployeeNumber("000123"), new EmployeeNumber("123"));
        }

        [Test]
        public void EmployeeNumber_RejectsMissingIdentity()
        {
            Assert.Throws<ArgumentException>(() => new EmployeeNumber(" "));
            Assert.Throws<ArgumentException>(() => new EmployeeNumber(null));
        }

        [Test]
        public void EmployeePin_PreservesLeadingZeroesAndDoesNotLeakInToString()
        {
            var pin = new EmployeePin("00123");
            Assert.AreEqual("00123", pin.Value);
            Assert.AreNotEqual(new EmployeePin("123"), pin);
            Assert.AreEqual("[REDACTED]", pin.ToString());
        }

        [Test]
        public void EmployeePin_RejectsMissingPin()
        {
            Assert.Throws<ArgumentException>(() => new EmployeePin(""));
            Assert.Throws<ArgumentException>(() => new EmployeePin(null));
        }

        [Test]
        public void Employee_RequiresNamesAndDepartment()
        {
            Assert.Throws<InvalidOperationException>(() => new PtiEmployee(
                new EmployeeNumber("1"), new EmployeePin("123"), null, "Last", "pti", true));
            Assert.Throws<InvalidOperationException>(() => new PtiEmployee(
                new EmployeeNumber("1"), new EmployeePin("123"), "First", "Last", "", true));
        }

        [Test]
        public void Employee_DeactivationPreservesAllOtherProperties()
        {
            var active = Employee();
            var inactive = active.WithActive(false);
            Assert.False(inactive.Active);
            Assert.True(active.Active);
            Assert.AreSame(active.Pin, inactive.Pin);
            Assert.AreSame(active.Number, inactive.Number);
            Assert.AreEqual(active.FirstName, inactive.FirstName);
            Assert.AreEqual(active.LastName, inactive.LastName);
            Assert.AreEqual(active.DepartmentNumber, inactive.DepartmentNumber);
            Assert.AreEqual(active.Type, inactive.Type);
            Assert.AreEqual(active.UnrestrictedUsage, inactive.UnrestrictedUsage);
        }

        [Test]
        public async Task Synchronize_UsesSingleUpsertWithoutPreliminaryRead()
        {
            var gateway = new FakeGateway();
            var result = await new EmployeeSynchronizationService(gateway).SynchronizeAsync(Employee());
            Assert.AreEqual(0, gateway.Reads);
            Assert.AreEqual(1, gateway.Writes);
            Assert.AreEqual(EmployeeChangeKind.Updated, result.Change);
        }

        [Test]
        public async Task Deactivate_UpdatesOneExistingEmployeeAndPreservesPinAndFlags()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot() } };
            var result = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(new EmployeeNumber("99990001"), false);
            Assert.AreEqual(1, gateway.Reads);
            Assert.AreEqual(1, gateway.Writes);
            Assert.False(gateway.SavedEmployee.Active);
            Assert.AreEqual("00123", gateway.SavedEmployee.Pin.Value);
            Assert.AreEqual("staff", gateway.SavedEmployee.Type);
            Assert.AreEqual(true, gateway.SavedEmployee.UnrestrictedUsage);
            Assert.AreEqual(EmployeeChangeKind.Updated, result.Change);
        }

        [Test]
        public async Task Reactivate_ChangesOnlyActivationFlag()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot() } };
            await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(new EmployeeNumber("99990001"), true);
            Assert.True(gateway.SavedEmployee.Active);
            Assert.AreEqual("00123", gateway.SavedEmployee.Pin.Value);
        }

        [Test]
        public void Deactivate_RefusesAmbiguousEmployeeNumber()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot(), Snapshot() } };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new EmployeeSynchronizationService(gateway)
                    .ChangeActivationAsync(new EmployeeNumber("99990001"), false));
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public void Deactivate_RefusesMissingEmployee()
        {
            var gateway = new FakeGateway();
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new EmployeeSynchronizationService(gateway)
                    .ChangeActivationAsync(new EmployeeNumber("99990001"), false));
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public void Deactivate_RefusesDeletedEmployee()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot(deleted: true) } };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new EmployeeSynchronizationService(gateway)
                    .ChangeActivationAsync(new EmployeeNumber("99990001"), false));
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public void Deactivate_RefusesIncompleteRemoteRecord()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot(pin: null) } };
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await new EmployeeSynchronizationService(gateway)
                    .ChangeActivationAsync(new EmployeeNumber("99990001"), false));
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public void Deactivate_IgnoresDeletedDuplicate()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot(deleted: true), Snapshot() } };
            Assert.DoesNotThrowAsync(async () =>
                await new EmployeeSynchronizationService(gateway)
                    .ChangeActivationAsync(new EmployeeNumber("99990001"), false));
            Assert.AreEqual(1, gateway.Writes);
        }

        [Test]
        public void Snapshot_ReadModelAllowsPartialDataButCannotWriteIt()
        {
            var snapshot = new PtiEmployeeSnapshot("99990001", null, null, null, null, false, false);
            Assert.Throws<InvalidOperationException>(() => snapshot.ToEmployee());
        }
    }
}
