using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using LGC.PTI.EmployeeSync.Application;
using LGC.PTI.EmployeeSync.Domain;
using LGC.PTI.GraphQL.Transport;

namespace LGC.PTI.HTTPClient.GraphQL
{
    /// <summary>
    /// GraphQL anti-corruption adapter. Expected API/domain errors return Fin.Fail.
    /// Unexpected network exceptions are handled by the Aff effect boundary.
    /// </summary>
    public sealed class GraphQLEmployeeGateway : IPtiEmployeeGateway
    {
        private readonly IPtiGraphQLClient _client;

        public GraphQLEmployeeGateway(IPtiGraphQLClient client) => _client = client;

        public Aff<IReadOnlyList<PtiEmployeeSnapshot>> GetEmployeesAsync(
            CancellationToken cancellationToken = default)
        {
            if (_client == null)
                return Prelude.FailAff<IReadOnlyList<PtiEmployeeSnapshot>>(
                    Error.New("GraphQL client is not configured."));

            return Prelude.AffMaybe(() => GetEmployeesCoreAsync(cancellationToken));
        }

        private async ValueTask<Fin<IReadOnlyList<PtiEmployeeSnapshot>>> GetEmployeesCoreAsync(CancellationToken ct)
        {
            var employees = await _client.GetEmployeesAsync(ct).ConfigureAwait(false);
            if (employees == null || employees.Any(employee => employee == null))
                return Prelude.FinFail<IReadOnlyList<PtiEmployeeSnapshot>>(
                    Error.New("PTI GraphQL returned an invalid employee collection."));

            var snapshots = employees.Select(ToSnapshot).ToList();
            return Prelude.FinSucc<IReadOnlyList<PtiEmployeeSnapshot>>(snapshots);
        }

        public Aff<EmployeeSyncOutcome> SaveAsync(
            PtiEmployee employee, CancellationToken cancellationToken = default)
        {
            if (_client == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("GraphQL client is not configured."));
            if (employee == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("Employee is required."));

            return Prelude.AffMaybe(() => SaveCoreAsync(employee, cancellationToken));
        }

        private async ValueTask<Fin<EmployeeSyncOutcome>> SaveCoreAsync(PtiEmployee employee, CancellationToken ct)
        {
            var input = new EmployeeInput
            {
                Number = employee.Number.Value,
                Pin = employee.Pin.Value,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentNumber = employee.DepartmentNumber,
                Active = employee.Active,
                Type = employee.Type,
                UnrestrictedUsage = employee.UnrestrictedUsage
            };

            var result = await _client.AddOrUpdateEmployeesAsync(new[] { input }, ct).ConfigureAwait(false);
            if (result == null || result.Count != 1)
                return Prelude.FinFail<EmployeeSyncOutcome>(
                    Error.New("PTI GraphQL upsert did not return exactly one employee."));

            var saved = result[0];
            if (saved == null || saved.Deleted || !string.IsNullOrWhiteSpace(saved.Error) ||
                !string.Equals(saved.Number, employee.Number.Value, StringComparison.OrdinalIgnoreCase))
                return Prelude.FinFail<EmployeeSyncOutcome>(
                    Error.New("PTI GraphQL returned an invalid employee upsert result."));

            EmployeeChangeKind change;
            switch (saved.Status)
            {
                case "new":
                    change = EmployeeChangeKind.Created;
                    break;
                case "updated":
                    change = EmployeeChangeKind.Updated;
                    break;
                default:
                    return Prelude.FinFail<EmployeeSyncOutcome>(
                        Error.New("PTI GraphQL returned an unrecognized employee operation status."));
            }

            return EmployeeSyncOutcome.Create(employee.Number, change, ToSnapshot(saved));
        }

        private static PtiEmployeeSnapshot ToSnapshot(Employee employee) =>
            new PtiEmployeeSnapshot(
                employee.Number, employee.Pin, employee.FirstName,
                employee.LastName, employee.DepartmentNumber, employee.Active,
                employee.Deleted, employee.Type, employee.UnrestrictedUsage);
    }
}
