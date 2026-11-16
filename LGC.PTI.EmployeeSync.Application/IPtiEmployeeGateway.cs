using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    /// <summary>
    /// Outbound port. The application knows nothing about GraphQL, REST, HTTP or DTOs.
    /// </summary>
    public interface IPtiEmployeeGateway
    {
        Task<IReadOnlyList<PtiEmployeeSnapshot>> GetEmployeesAsync(CancellationToken cancellationToken = default);
        Task<EmployeeSyncOutcome> SaveAsync(PtiEmployee employee, CancellationToken cancellationToken = default);
    }
}
