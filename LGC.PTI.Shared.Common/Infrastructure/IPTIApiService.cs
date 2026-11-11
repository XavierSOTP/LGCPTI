using LGC.PTI.Common.Application.Dtos;
using SoftSolute.Common.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LGC.PTI.Domain.GraphQLClient
{
    public interface IPTIApiService
    {
        Task<Option<TProjection>> AddOrUpdateEmployees<TProjection>(EmployeeInputDto employeeInput) where TProjection : BaseEmployeeDto;
        Task<IEnumerable<TProjection>> GetAllEmployees<TProjection>() where TProjection : BaseEmployeeDto;
    }
}