using System.Collections.Generic;

namespace LGC.PTI.Common.Application.Dtos
{
    /// <summary>
    /// Response received from PTI when mutating employees
    /// </summary>
    /// <typeparam name="TProjection"></typeparam>
    public class AddOrUpdateEmployeesResponse<TProjection> where TProjection : BaseEmployeeDto
    {
        public List<TProjection> AddOrUpdateEmployees { get; set; }
    }
}
