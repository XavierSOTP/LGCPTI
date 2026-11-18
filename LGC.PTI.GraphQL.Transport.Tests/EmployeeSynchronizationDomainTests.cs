using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using LGC.PTI.EmployeeSync.Application;
using LGC.PTI.EmployeeSync.Domain;
using NUnit.Framework;

namespace LGC.PTI.GraphQL.Transport.Tests
{
    [TestFixture]
    public class EmployeeSynchronizationDomainTests
    {
        private static PtiEmployee Employee() =>
            PtiEmployee.Create("99990001", "00123", "Genetec",
                "IntegrationTest", "pti", true, "staff", true).IfFail(default(PtiEmployee));

        private static EmployeeNumber Number(string value = "99990001") =>
            EmployeeNumber.Create(value).IfFail(default(EmployeeNumber));

        private static PtiEmployeeSnapshot Snapshot(
            string number = "99990001", bool deleted = false, string pin = "00123") =>
            new PtiEmployeeSnapshot(number, pin, "Genetec", "IntegrationTest",
                "pti", true, deleted, "staff", true);

        private sealed class FakeGateway : IPtiEmployeeGateway
        {
            public IReadOnlyList<PtiEmployeeSnapshot> RemoteEmployees { get; set; } =
                Array.Empty<PtiEmployeeSnapshot>();
            public PtiEmployee SavedEmployee { get; private set; }
            public int Reads { get; private set; }
            public int Writes { get; private set; }

            public Aff<IReadOnlyList<PtiEmployeeSnapshot>> GetEmployeesAsync(
                CancellationToken cancellationToken = default) =>
                Prelude.Aff(() =>
                {
                    Reads++;
                    return new ValueTask<IReadOnlyList<PtiEmployeeSnapshot>>(RemoteEmployees);
                });

            public Aff<EmployeeSyncOutcome> SaveAsync(
                PtiEmployee employee, CancellationToken cancellationToken = default) =>
                Prelude.AffMaybe(() =>
                {
                    SavedEmployee = employee;
                    Writes++;
                    var result = new PtiEmployeeSnapshot(
                        employee.Number.Value, employee.Pin.Value, employee.FirstName,
                        employee.LastName, employee.DepartmentNumber, employee.Active,
                        false, employee.Type, employee.UnrestrictedUsage);
                    return new ValueTask<Fin<EmployeeSyncOutcome>>(
                        EmployeeSyncOutcome.Create(employee.Number, EmployeeChangeKind.Updated, result));
                });
        }

        [Test]
        public void EmployeeNumber_PreservesFormattingAndValueEquality()
        {
            var lower = EmployeeNumber.Create("abc001").IfFail(default(EmployeeNumber));
            var upper = EmployeeNumber.Create("ABC001").IfFail(default(EmployeeNumber));
            Assert.AreEqual(lower, upper);
            Assert.AreEqual(lower.GetHashCode(), upper.GetHashCode());
            Assert.AreNotEqual(Number("000123"), Number("123"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void EmployeeNumber_InvalidValueReturnsFinFail(string value)
        {
            Assert.True(EmployeeNumber.Create(value).IsFail);
        }

        [Test]
        public void EmployeePin_PreservesLeadingZeroesAndMasksToString()
        {
            var pin = EmployeePin.Create("00123").IfFail(default(EmployeePin));
            var differentPin = EmployeePin.Create("123").IfFail(default(EmployeePin));
            Assert.AreEqual("00123", pin.Value);
            Assert.AreNotEqual(differentPin, pin);
            Assert.AreEqual("[REDACTED]", pin.ToString());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void EmployeePin_InvalidValueReturnsFinFail(string value)
        {
            Assert.True(EmployeePin.Create(value).IsFail);
        }

        [Test]
        public void Employee_MissingNameOrDepartmentReturnsFinFail()
        {
            Assert.True(PtiEmployee.Create("1", "123", null, "Last", "pti", true).IsFail);
            Assert.True(PtiEmployee.Create("1", "123", "First", "Last", "", true).IsFail);
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
            var fin = await new EmployeeSynchronizationService(gateway)
                .SynchronizeAsync(Employee()).Run();
            Assert.True(fin.IsSucc);
            Assert.AreEqual(0, gateway.Reads);
            Assert.AreEqual(1, gateway.Writes);
            Assert.AreEqual(EmployeeChangeKind.Updated,
                fin.IfFail(default(EmployeeSyncOutcome)).Change);
        }

        [Test]
        public async Task Synchronize_NullEmployeeReturnsFinFailWithoutWriting()
        {
            var gateway = new FakeGateway();
            var fin = await new EmployeeSynchronizationService(gateway)
                .SynchronizeAsync(null).Run();
            Assert.True(fin.IsFail);
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public async Task Deactivate_UpdatesOneExistingEmployeeAndPreservesDetails()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot() } };
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), false).Run();
            Assert.True(fin.IsSucc);
            Assert.AreEqual(1, gateway.Reads);
            Assert.AreEqual(1, gateway.Writes);
            Assert.False(gateway.SavedEmployee.Active);
            Assert.AreEqual("00123", gateway.SavedEmployee.Pin.Value);
            Assert.AreEqual("staff", gateway.SavedEmployee.Type);
            Assert.AreEqual(true, gateway.SavedEmployee.UnrestrictedUsage);
        }

        [Test]
        public async Task Reactivate_ChangesOnlyActivationFlag()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot() } };
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), true).Run();
            Assert.True(fin.IsSucc);
            Assert.True(gateway.SavedEmployee.Active);
            Assert.AreEqual("00123", gateway.SavedEmployee.Pin.Value);
        }

        [Test]
        public async Task Deactivate_NullIdentityReturnsFinFailWithoutReadOrWrite()
        {
            var gateway = new FakeGateway();
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(null, false).Run();
            Assert.True(fin.IsFail);
            Assert.AreEqual(0, gateway.Reads);
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public async Task Deactivate_AmbiguousNumberReturnsFinFailWithoutWriting()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot(), Snapshot() } };
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), false).Run();
            Assert.True(fin.IsFail);
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public async Task Deactivate_MissingEmployeeReturnsFinFailWithoutWriting()
        {
            var gateway = new FakeGateway();
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), false).Run();
            Assert.True(fin.IsFail);
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public async Task Deactivate_DeletedEmployeeReturnsFinFailWithoutWriting()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot(deleted: true) } };
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), false).Run();
            Assert.True(fin.IsFail);
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public async Task Deactivate_IncompleteRecordReturnsFinFailWithoutWriting()
        {
            var gateway = new FakeGateway { RemoteEmployees = new[] { Snapshot(pin: null) } };
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), false).Run();
            Assert.True(fin.IsFail);
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public async Task Deactivate_IgnoresDeletedDuplicate()
        {
            var gateway = new FakeGateway
            {
                RemoteEmployees = new[] { Snapshot(deleted: true), Snapshot() }
            };
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), false).Run();
            Assert.True(fin.IsSucc);
            Assert.AreEqual(1, gateway.Writes);
        }

        [Test]
        public async Task Deactivate_NullRemoteCollectionReturnsFinFail()
        {
            var gateway = new FakeGateway { RemoteEmployees = null };
            var fin = await new EmployeeSynchronizationService(gateway)
                .ChangeActivationAsync(Number(), false).Run();
            Assert.True(fin.IsFail);
            Assert.AreEqual(0, gateway.Writes);
        }

        [Test]
        public void Snapshot_IncompleteOrDeletedRecordReturnsFinFail()
        {
            var incomplete = new PtiEmployeeSnapshot(
                "99990001", null, null, null, null, false, false);
            Assert.True(incomplete.ToEmployee().IsFail);
            Assert.True(Snapshot(deleted: true).ToEmployee().IsFail);
        }

        [Test]
        public void Outcome_MissingDataReturnsFinFail()
        {
            Assert.True(EmployeeSyncOutcome.Create(null, EmployeeChangeKind.Updated, Snapshot()).IsFail);
            Assert.True(EmployeeSyncOutcome.Create(Number(), EmployeeChangeKind.Updated, null).IsFail);
        }

        [Test]
        public async Task Service_MissingGatewayReturnsFinFailWithoutThrowing()
        {
            var service = new EmployeeSynchronizationService(null);
            Assert.True((await service.SynchronizeAsync(Employee()).Run()).IsFail);
            Assert.True((await service.ChangeActivationAsync(Number(), false).Run()).IsFail);
        }
    }
}
