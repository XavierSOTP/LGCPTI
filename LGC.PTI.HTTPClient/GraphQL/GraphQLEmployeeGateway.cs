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
using SoftSolute.Utiities.Plugin.Adapters;

namespace LGC.PTI.HTTPClient.GraphQL
{
    /// <summary>
    /// GraphQL anti-corruption adapter. Validation and transport failures are captured
    /// as Fin.Fail by LanguageExtAffExtensions.Aff, using the runtime-compatible Prelude.Aff.
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

            return LanguageExtAffExtensions.Aff<IReadOnlyList<PtiEmployeeSnapshot>>(
                () => GetEmployeesCoreAsync(cancellationToken));
        }

        private async Task<IReadOnlyList<PtiEmployeeSnapshot>> GetEmployeesCoreAsync(CancellationToken ct)
        {
            var employees = await _client.GetEmployeesAsync(ct).ConfigureAwait(false);
            if (employees == null || employees.Any(employee => employee == null))
                throw new InvalidOperationException("PTI GraphQL returned an invalid employee collection.");

            return employees.Select(ToSnapshot).ToList();
        }

        public Aff<EmployeeSyncOutcome> SaveAsync(
            PtiEmployee employee, CancellationToken cancellationToken = default)
        {
            if (_client == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("GraphQL client is not configured."));
            if (employee == null)
                return Prelude.FailAff<EmployeeSyncOutcome>(Error.New("Employee is required."));

            return LanguageExtAffExtensions.Aff<EmployeeSyncOutcome>(
                () => SaveCoreAsync(employee, cancellationToken));
        }

        private async Task<EmployeeSyncOutcome> SaveCoreAsync(PtiEmployee employee, CancellationToken ct)
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
                throw new InvalidOperationException("PTI GraphQL upsert did not return exactly one employee.");

            var saved = result[0];
            if (saved == null || saved.Deleted != false || !string.IsNullOrWhiteSpace(saved.Error) ||
                !string.Equals(saved.Number, employee.Number.Value, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PTI GraphQL returned an invalid employee upsert result.");

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
                    throw new InvalidOperationException("PTI GraphQL returned an unrecognized employee operation status.");
            }

            return EmployeeSyncOutcome.Create(employee.Number, change, ToSnapshot(saved)).Match(
                Fail: error => throw new InvalidOperationException(error.Message),
                Succ: outcome => outcome);
        }

        private static PtiEmployeeSnapshot ToSnapshot(Employee employee) =>
            new PtiEmployeeSnapshot(
                employee.Number, employee.Pin, employee.FirstName,
                employee.LastName, employee.DepartmentNumber, employee.Active,
                employee.Deleted, employee.Type, employee.UnrestrictedUsage);
    }
}
