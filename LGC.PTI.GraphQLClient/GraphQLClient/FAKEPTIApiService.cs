using LGC.PTI.Common.Application.Dtos;
using LGC.PTI.Domain.GraphQLClient;
using MapsterMapper;
using NLog;
using SoftSolute.Common.Utilities;
using SoftSolute.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LGC.PTI.Infrastructure.GraphQLClient
{
    internal class FAKEPTIApiService : IPTIApiService
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public FAKEPTIApiService(ILogger logger, IMapper mapper)
        {
            _logger = logger.Factory.GetCurrentClassLogger();
            _mapper = mapper;
        }
        public Task<Option<TProjection>> AddOrUpdateEmployees<TProjection>(EmployeeInputDto employeeInput) where TProjection : BaseEmployeeDto
        {
            _logger.LogEnteringMethod();
            _logger.InfoAsJson(employeeInput);
            var result = _mapper.Map<TProjection>(employeeInput);
            return Task.FromResult((Option<TProjection>)result);
        }

        public Task<IEnumerable<TProjection>> GetAllEmployees<TProjection>() where TProjection : BaseEmployeeDto
        {
            _logger.LogEnteringMethod();
            return Task.FromResult(System.Array.Empty<TProjection>().AsEnumerable());
        }
    }
}