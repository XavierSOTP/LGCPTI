using LanguageExt;
using LGC.PTI.Common.Application.Dtos;
using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.EmployeeSync.Application;
using LGC.PTI.EmployeeSync.Domain;
using LGC.PTI.GraphQL.Transport;
using LGC.PTI.HTTPClient.Refit.Employees;
using SoftSolute.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LGC.PTI.HTTPClient.GraphQL
{
    /// <summary>
    /// Legacy IEmployeeApiSrv inbound adapter. It translates REST-shaped requests to the
    /// PTI synchronization use cases without leaking GraphQL into the domain.
    /// Not registered in DI until the migration is ready.
    /// </summary>
    public sealed class GraphQLEmployeeApiSrv : IEmployeeApiSrv
    {
        private readonly IPtiGraphQLClient _client;
        private readonly EmployeeSynchronizationService _synchronization;

        public GraphQLEmployeeApiSrv(IPtiGraphQLClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _synchronization = new EmployeeSynchronizationService(new GraphQLEmployeeGateway(client));
        }

        public Aff<IReadOnlyCollection<EmployeeInDto>> TryGetEmployees() =>
            AsAff<IReadOnlyCollection<EmployeeInDto>>(async () =>
            {
                var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
                return employees.Select(ToDto).ToList();
            });

        public Aff<EmployeeInDto> TryGetEmployee(NotEmptyString employeeId) =>
            AsAff<EmployeeInDto>(async () =>
            {
                var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
                var found = employees.FirstOrDefault(x =>
                    string.Equals(x.Number, employeeId, StringComparison.OrdinalIgnoreCase));
                // Preserve the existing REST compatibility contract until EmployeeManager is migrated.
                return found == null ? new EmployeeInDto() : ToDto(found);
            });

        public Aff<Option<EmployeeInDto>> TryGetEmployeeOptional(NotEmptyString employeeId) =>
            AsAff<Option<EmployeeInDto>>(async () =>
            {
                var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
                var found = employees.FirstOrDefault(x =>
                    string.Equals(x.Number, employeeId, StringComparison.OrdinalIgnoreCase));
                return found == null ? Option<EmployeeInDto>.None : (Option<EmployeeInDto>)ToDto(found);
            });

        public Aff<EmployeesResponse> TryCreateEmployee(NotNull<EmployeeOutDto> employee) => Synchronize(employee.Value);

        public Aff<EmployeesResponse> TryUpdateEmployee(NotNull<EmployeeOutDto> employee) => Synchronize(employee.Value);

        public Aff<EmployeesResponse> TryUpdateEmployeeStatus(NotNull<EmployeeStatusOutDto> status) =>
            AsAff<EmployeesResponse>(async () =>
            {
                var result = await _synchronization.ChangeActivationAsync(
                    new EmployeeNumber(status.Value.EmployeeNumber),
                    status.Value.Active).ConfigureAwait(false);

                return ToResponse(result);
            });

        private Aff<EmployeesResponse> Synchronize(EmployeeOutDto source) =>
            AsAff<EmployeesResponse>(async () =>
            {
                var employee = ToDomain(source);
                var result = await _synchronization.SynchronizeAsync(employee).ConfigureAwait(false);
                return ToResponse(result);
            });

        /// <summary>
        /// Wraps an asynchronous use case in a LanguageExt effect without depending on
        /// SoftSolute.Utiities.Plugin, which targets .NET Framework rather than netstandard2.0.
        /// </summary>
        private static Aff<T> AsAff<T>(Func<System.Threading.Tasks.Task<T>> operation)
        {
            return Prelude.Aff(() => new System.Threading.Tasks.ValueTask<T>(operation()));
        }

        private static PtiEmployee ToDomain(EmployeeOutDto source)
        {
            // Existing REST DTOs stay in this inbound anti-corruption adapter.
            if (source == null || string.IsNullOrWhiteSpace(source.EmployeeNumber) ||
                string.IsNullOrWhiteSpace(source.Pin) || string.IsNullOrWhiteSpace(source.FirstName) ||
                string.IsNullOrWhiteSpace(source.LastName) || string.IsNullOrWhiteSpace(source.Department))
                throw new InvalidOperationException("Cannot send employee to PTI GraphQL: a required field is missing.");

            return new PtiEmployee(
                new EmployeeNumber(source.EmployeeNumber),
                new EmployeePin(source.Pin),
                source.FirstName,
                source.LastName,
                source.Department,
                source.Active);
        }

        private static EmployeeInDto ToDto(Employee source) => new EmployeeInDto
        {
            EmployeeNumber = source.Number,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Department = source.DepartmentNumber,
            Active = source.Active
        };

        private static EmployeeInDto ToDto(PtiEmployeeSnapshot source) => new EmployeeInDto
        {
            EmployeeNumber = source.Number,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Department = source.DepartmentNumber,
            Active = source.Active
        };

        private static EmployeesResponse ToResponse(EmployeeSyncOutcome outcome)
        {
            return new EmployeesResponse
            {
                IsSuccess = true,
                Errors = new List<string>(),
                OperationStatus = outcome.Change == EmployeeChangeKind.Created ? "new" : "updated",
                Result = new LGC.PTI.Common.Application.Dtos.Result
                {
                    Items = new List<EmployeeInDto> { ToDto(outcome.Employee) },
                    MetaData = new MetaData
                    {
                        HasNextPage = false,
                        PageCount = 1,
                        PageNumber = 1,
                        PageSize = 1,
                        TotalItemCount = 1
                    }
                }
            };
        }
    }
}
