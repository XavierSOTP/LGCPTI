using System.Collections.Generic;
using System.Threading;
using LanguageExt;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    /// <summary>Outbound port. The application does not know about GraphQL, REST or HTTP.</summary>
    public interface IPtiEmployeeGateway
    {
        Aff<IReadOnlyList<PtiEmployeeSnapshot>> GetEmployeesAsync(
            CancellationToken cancellationToken = default);

        Aff<EmployeeSyncOutcome> SaveAsync(
            PtiEmployee employee, CancellationToken cancellationToken = default);
    }
}
