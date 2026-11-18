using LanguageExt;
using LanguageExt.Common;
using LGC.PTI.Common.Application.Dtos;
using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.EmployeeSync.Application;
using LGC.PTI.EmployeeSync.Domain;
using LGC.PTI.GraphQL.Transport;
using LGC.PTI.HTTPClient.Refit.Employees;
using SoftSolute.Common.Utilities;
using SoftSolute.Utiities.Plugin.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LGC.PTI.HTTPClient.GraphQL
{
    /// <summary>
    /// Legacy IEmployeeApiSrv inbound adapter. It translates REST-shaped requests to the
    /// PTI synchronization use cases without leaking GraphQL into the domain.
    /// Registered through GraphQLClientModule once the PTI GraphQL integration is enabled.
    /// </summary>
    public sealed class GraphQLEmployeeApiSrv : IEmployeeApiSrv
    {
        private readonly IPtiGraphQLClient _client;
        private readonly EmployeeSynchronizationService _synchronization;

        public GraphQLEmployeeApiSrv(IPtiGraphQLClient client)
        {
            _client = client;
            _synchronization = new EmployeeSynchronizationService(new GraphQLEmployeeGateway(client));
        }

        public Aff<IReadOnlyCollection<EmployeeInDto>> TryGetEmployees()
        {
            if (_client == null)
                return Prelude.FailAff<IReadOnlyCollection<EmployeeInDto>>(Error.New("GraphQL client is not configured."));

            return LanguageExtAffExtensions.Aff<IReadOnlyCollection<EmployeeInDto>>(async () =>
            {
                var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
                return employees.Select(ToDto).ToList();
            });
        }

        public Aff<EmployeeInDto> TryGetEmployee(NotEmptyString employeeId)
        {
            if (_client == null)
                return Prelude.FailAff<EmployeeInDto>(Error.New("GraphQL client is not configured."));

            return LanguageExtAffExtensions.Aff<EmployeeInDto>(async () =>
            {
                var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
                var found = employees.FirstOrDefault(x =>
                    string.Equals(x.Number, employeeId, StringComparison.OrdinalIgnoreCase));
                // Preserve the legacy contract until EmployeeManager is migrated.
                return found == null ? new EmployeeInDto() : ToDto(found);
            });
        }

        public Aff<Option<EmployeeInDto>> TryGetEmployeeOptional(NotEmptyString employeeId)
        {
            if (_client == null)
                return Prelude.FailAff<Option<EmployeeInDto>>(Error.New("GraphQL client is not configured."));

            return LanguageExtAffExtensions.Aff<Option<EmployeeInDto>>(async () =>
            {
                var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
                var found = employees.FirstOrDefault(x =>
                    string.Equals(x.Number, employeeId, StringComparison.OrdinalIgnoreCase));
                return found == null ? Option<EmployeeInDto>.None : (Option<EmployeeInDto>)ToDto(found);
            });
        }

        public Aff<EmployeesResponse> TryCreateEmployee(NotNull<EmployeeOutDto> employee) => Synchronize(employee.Value);

        public Aff<EmployeesResponse> TryUpdateEmployee(NotNull<EmployeeOutDto> employee) => Synchronize(employee.Value);

        public Aff<EmployeesResponse> TryUpdateEmployeeStatus(NotNull<EmployeeStatusOutDto> status)
        {
            if (status.Value == null)
                return Prelude.FailAff<EmployeesResponse>(Error.New("Employee status is required."));

            return EmployeeNumber.Create(status.Value.EmployeeNumber).Match(
                Fail: error => Prelude.FailAff<EmployeesResponse>(error),
                Succ: number => _synchronization.ChangeActivationAsync(number, status.Value.Active)
                    .Map(ToResponse));
        }

        private Aff<EmployeesResponse> Synchronize(EmployeeOutDto source) =>
            ToDomain(source).Match(
                Fail: error => Prelude.FailAff<EmployeesResponse>(error),
                Succ: employee => _synchronization.SynchronizeAsync(employee).Map(ToResponse));

        private static Fin<PtiEmployee> ToDomain(EmployeeOutDto source)
        {
            if (source == null)
                return Prelude.FinFail<PtiEmployee>(Error.New("Employee is required."));

            // Legacy REST DTOs are converted to validated domain values at this boundary.
            return PtiEmployee.Create(
                source.EmployeeNumber, source.Pin, source.FirstName,
                source.LastName, source.Department, source.Active);
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
