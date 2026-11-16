using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    /// <summary>Application use cases: synchronize employee and change activation.</summary>
    public sealed class EmployeeSynchronizationService
    {
        private readonly IPtiEmployeeGateway _gateway;

        public EmployeeSynchronizationService(IPtiEmployeeGateway gateway)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        /// <summary>Upsert without a preliminary GET; the PTI gateway reports created/updated.</summary>
        public Task<EmployeeSyncOutcome> SynchronizeAsync(
            PtiEmployee employee, CancellationToken cancellationToken = default)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));
            return _gateway.SaveAsync(employee, cancellationToken);
        }

        /// <summary>
        /// Only one non-deleted record may be changed. Preserve all mandatory and optional fields.
        /// Never invoke deleteEmployees.
        /// </summary>
        public async Task<EmployeeSyncOutcome> ChangeActivationAsync(
            EmployeeNumber number, bool active, CancellationToken cancellationToken = default)
        {
            if (number == null) throw new ArgumentNullException(nameof(number));

            var employees = await _gateway.GetEmployeesAsync(cancellationToken).ConfigureAwait(false);
            if (employees == null) throw new InvalidOperationException("PTI did not return employee records.");

            var matches = employees
                .Where(employee => employee != null && !employee.Deleted &&
                    string.Equals(employee.Number, number.Value, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count != 1)
                throw new InvalidOperationException(
                    $"Cannot safely update PTI employee status for {number.Value}: found {matches.Count} non-deleted matches.");

            var updatedEmployee = matches[0].ToEmployee().WithActive(active);
            return await _gateway.SaveAsync(updatedEmployee, cancellationToken).ConfigureAwait(false);
        }
    }
}
