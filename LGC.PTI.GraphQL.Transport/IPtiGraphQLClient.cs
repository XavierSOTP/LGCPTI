using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LGC.PTI.GraphQL.Transport
{
    /// <summary>GraphQL employee operations implemented by PtiGraphQLClient and replaceable in unit tests.</summary>
    public interface IPtiGraphQLClient
    {
        Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Employee>> AddOrUpdateEmployeesAsync(IReadOnlyCollection<EmployeeInput> employees, CancellationToken cancellationToken = default);
    }
}
