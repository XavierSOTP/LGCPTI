using LanguageExt;
using LGC.PTI.Common.Application.Dtos;
using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.GraphQL.Transport;
using LGC.PTI.HTTPClient.Refit.Employees;
using SoftSolute.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LGC.PTI.HTTPClient.GraphQL
{
    /// <summary>
    /// Adapts the existing IEmployeeApiSrv contract to the new PTI GraphQL API.
    /// This service is deliberately not registered in DI until the migration is ready.
    /// </summary>
    public sealed class GraphQLEmployeeApiSrv : IEmployeeApiSrv
    {
        private readonly IPtiGraphQLClient _client;

        public GraphQLEmployeeApiSrv(IPtiGraphQLClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Aff<IReadOnlyCollection<EmployeeInDto>> TryGetEmployees() => LanguageExtAffExtensions.Aff<IReadOnlyCollection<EmployeeInDto>>(async () =>
        {
            var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
            return employees.Select(ToDto).ToList();
        });

        public Aff<EmployeeInDto> TryGetEmployee(NotEmptyString employeeId) => LanguageExtAffExtensions.Aff<EmployeeInDto>(async () =>
        {
            var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
            var found = employees.FirstOrDefault(x => string.Equals(x.Number, employeeId, StringComparison.OrdinalIgnoreCase));
            return found == null ? new EmployeeInDto() : ToDto(found);
        });

        public Aff<Option<EmployeeInDto>> TryGetEmployeeOptional(NotEmptyString employeeId) => LanguageExtAffExtensions.Aff<Option<EmployeeInDto>>(async () =>
        {
            var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
            var found = employees.FirstOrDefault(x => string.Equals(x.Number, employeeId, StringComparison.OrdinalIgnoreCase));
            return found == null ? Option<EmployeeInDto>.None : (Option<EmployeeInDto>)ToDto(found);
        });

        public Aff<EmployeesResponse> TryCreateEmployee(NotNull<EmployeeOutDto> employee) => Upsert(employee.Value);

        public Aff<EmployeesResponse> TryUpdateEmployee(NotNull<EmployeeOutDto> employee) => Upsert(employee.Value);

        public Aff<EmployeesResponse> TryUpdateEmployeeStatus(NotNull<EmployeeStatusOutDto> status) => LanguageExtAffExtensions.Aff<EmployeesResponse>(async () =>
        {
            var employeeNumber = status.Value.EmployeeNumber;
            var employees = await _client.GetEmployeesAsync().ConfigureAwait(false);
            var matches = employees.Where(x => !x.Deleted && string.Equals(x.Number, employeeNumber, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count != 1) throw new InvalidOperationException($"Cannot safely update PTI employee status for {employeeNumber}: found {matches.Count} non-deleted matches.");

            var existing = matches[0];
            var input = new EmployeeInput
            {
                Number = existing.Number,
                Pin = existing.Pin,
                FirstName = existing.FirstName,
                LastName = existing.LastName,
                DepartmentNumber = existing.DepartmentNumber,
                Active = status.Value.Active,
                Type = existing.Type,
                UnrestrictedUsage = existing.UnrestrictedUsage
            };

            Validate(input);
            var result = await _client.AddOrUpdateEmployeesAsync(new[] { input }).ConfigureAwait(false);
            return ToResponse(result, employeeNumber);
        });

        private Aff<EmployeesResponse> Upsert(EmployeeOutDto employee) => LanguageExtAffExtensions.Aff<EmployeesResponse>(async () =>
        {
            var input = new EmployeeInput
            {
                Number = employee.EmployeeNumber,
                Pin = employee.Pin,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DepartmentNumber = employee.Department,
                Active = employee.Active
            };

            Validate(input);
            var result = await _client.AddOrUpdateEmployeesAsync(new[] { input }).ConfigureAwait(false);
            return ToResponse(result, employee.EmployeeNumber);
        });

        private static EmployeeInDto ToDto(Employee source) => new EmployeeInDto
        {
            EmployeeNumber = source.Number,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Department = source.DepartmentNumber,
            Active = source.Active
        };

        private static void Validate(EmployeeInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Number) || string.IsNullOrWhiteSpace(input.Pin) ||
                string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.LastName) ||
                string.IsNullOrWhiteSpace(input.DepartmentNumber))
                throw new InvalidOperationException("Cannot send employee to PTI GraphQL: a required field is missing.");
        }

        private static EmployeesResponse ToResponse(IReadOnlyList<Employee> employees, string requestedNumber)
        {
            if (employees == null || employees.Count != 1) throw new InvalidOperationException("PTI GraphQL upsert did not return exactly one employee.");
            var employee = employees[0];
            if (employee == null || employee.Deleted || !string.Equals(employee.Number, requestedNumber, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(employee.Error) || (employee.Status != "new" && employee.Status != "updated"))
                throw new InvalidOperationException("PTI GraphQL returned an invalid employee upsert result.");

            return new EmployeesResponse
            {
                IsSuccess = true,
                Errors = new List<string>(),
                OperationStatus = employee.Status,
                Result = new LGC.PTI.Common.Application.Dtos.Result
                {
                    Items = new List<EmployeeInDto> { ToDto(employee) },
                    MetaData = new MetaData { HasNextPage = false, PageCount = 1, PageNumber = 1, PageSize = 1, TotalItemCount = 1 }
                }
            };
        }
    }
}
