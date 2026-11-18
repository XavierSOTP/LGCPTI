using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LanguageExt;
using LanguageExt.Common;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    /// <summary>Monadic use cases: expected validation failures short-circuit as Fin.Fail.</summary>
    public sealed class EmployeeSynchronizationService
    {
        private readonly IPtiEmployeeGateway _gateway;

        public EmployeeSynchronizationService(IPtiEmployeeGateway gateway) => _gateway = gateway;

        /// <summary>Upsert directly, without reading all employees first.</summary>
        public Aff<EmployeeSyncOutcome> SynchronizeAsync(
            PtiEmployee employee, CancellationToken cancellationToken = default)
        {
            if (_gateway == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("PTI gateway is not configured."));
            if (employee == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("Employee is required."));

            return _gateway.SaveAsync(employee, cancellationToken);
        }

        /// <summary>
        /// Only an unambiguous, complete, non-deleted remote record may be activated/deactivated.
        /// Does not delete PTI employees or throw for expected validation failures.
        /// </summary>
        public Aff<EmployeeSyncOutcome> ChangeActivationAsync(
            EmployeeNumber number, bool active, CancellationToken cancellationToken = default)
        {
            if (_gateway == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("PTI gateway is not configured."));
            if (number == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("Employee number is required."));

            return from employees in _gateway.GetEmployeesAsync(cancellationToken)
                   from result in ValidateAndSave(employees, number, active, cancellationToken)
                   select result;
        }

        private Aff<EmployeeSyncOutcome> ValidateAndSave(
            IReadOnlyList<PtiEmployeeSnapshot> employees,
            EmployeeNumber number, bool active, CancellationToken ct)
        {
            if (employees == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("PTI did not return employee records."));

            var matches = employees
                .Where(employee => employee != null && !employee.Deleted &&
                    string.Equals(employee.Number, number.Value, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();

            if (matches.Count != 1)
                return Prelude.FailAff<EmployeeSyncOutcome>(
                    Error.New($"Cannot safely update PTI employee status for {number.Value}: found {matches.Count} matching records."));

            return matches[0].ToEmployee().Match(
                Fail: error => Prelude.FailAff<EmployeeSyncOutcome>(error),
                Succ: employee => _gateway.SaveAsync(employee.WithActive(active), ct));
        }
    }
}
