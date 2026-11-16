using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LGC.PTI.EmployeeSync.Application;
using LGC.PTI.EmployeeSync.Domain;
using LGC.PTI.GraphQL.Transport;

namespace LGC.PTI.HTTPClient.GraphQL
{
    /// <summary>
    /// Anti-corruption layer between the PTI synchronization model and GraphQL DTOs.
    /// No domain or application type depends on GraphQL.Client.
    /// </summary>
    public sealed class GraphQLEmployeeGateway : IPtiEmployeeGateway
    {
        private readonly IPtiGraphQLClient _client;

        public GraphQLEmployeeGateway(IPtiGraphQLClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<IReadOnlyList<PtiEmployeeSnapshot>> GetEmployeesAsync(CancellationToken cancellationToken = default)
        {
            var employees = await _client.GetEmployeesAsync(cancellationToken).ConfigureAwait(false);
            if (employees == null)
                throw new InvalidOperationException("PTI GraphQL did not return an employee collection.");
            return employees.Select(ToSnapshot).ToList();
        }

        public async Task<EmployeeSyncOutcome> SaveAsync(PtiEmployee employee, CancellationToken cancellationToken = default)
        {
            if (employee == null) throw new ArgumentNullException(nameof(employee));

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

            var result = await _client.AddOrUpdateEmployeesAsync(new[] { input }, cancellationToken).ConfigureAwait(false);
            if (result == null || result.Count != 1)
                throw new InvalidOperationException("PTI GraphQL upsert did not return exactly one employee.");

            var saved = result[0];
            if (saved == null || saved.Deleted || !string.IsNullOrWhiteSpace(saved.Error) ||
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

            return new EmployeeSyncOutcome(employee.Number, change, ToSnapshot(saved));
        }

        private static PtiEmployeeSnapshot ToSnapshot(Employee employee)
        {
            if (employee == null)
                throw new InvalidOperationException("PTI GraphQL returned a null employee record.");

            return new PtiEmployeeSnapshot(
                employee.Number,
                employee.Pin,
                employee.FirstName,
                employee.LastName,
                employee.DepartmentNumber,
                employee.Active,
                employee.Deleted,
                employee.Type,
                employee.UnrestrictedUsage);
        }
    }
}
