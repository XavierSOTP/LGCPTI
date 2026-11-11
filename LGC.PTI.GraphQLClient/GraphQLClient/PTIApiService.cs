using GraphQL;
using LGC.PTI.Common.Application.Dtos;
using LGC.PTI.Domain.GraphQLClient;
using NLog;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.CollectionSS;
using SoftSolute.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LGC.PTI.Infrastructure.GraphQLClient
{


    public class PTIApiService : IPTIApiService
    {
        private readonly ILogger _logger;
        private readonly IGraphQLClientSS _graphQLClient;

        public PTIApiService(ILogger logger, IGraphQLClientSS graphQLClient)
        {
            _logger = logger.Factory.GetCurrentClassLogger();
            _graphQLClient = graphQLClient;
        }
        private GraphQLRequest CreateGetAllEmployees<TProjection>()
        {
            return GraphQLRequestBuilder.CreateQuery<TProjection>(methodSyntax: "getEmployees");
        }


        public async Task<IEnumerable<TProjection>> GetAllEmployees<TProjection>()
            where TProjection : BaseEmployeeDto
        {
            var request = CreateGetAllEmployees<TProjection>();
            var response = await _graphQLClient.SendQueryAsync<EmployeesResponse<TProjection>>(request);
            return response
                .Map<GraphQLResponse<EmployeesResponse<TProjection>>, IEnumerable<TProjection>>(r => r.Data.GetEmployees)
                .GetCollectionOrDefault();

        }

        private GraphQLRequest CreateAddOrUpdateEmployees<TProjection>(EmployeeInputDto employee)
        {
            var variables = new { employees = employee };
            return GraphQLRequestBuilder.CreateQuery<TProjection>(methodSyntax: "addOrUpdateEmployees(employees:$employees)",
                querySyntax: "mutation addOrUpdateEmployees($employees:[EmployeeInput])",
                variables);
        }

        public async Task<Option<TProjection>> AddOrUpdateEmployees<TProjection>(EmployeeInputDto employeeInput) where TProjection : BaseEmployeeDto
        {
            var request = CreateAddOrUpdateEmployees<TProjection>(employeeInput);
            var response = await _graphQLClient.SendQueryAsync<AddOrUpdateEmployeesResponse<TProjection>>(request);
            return response
                .Map<GraphQLResponse<AddOrUpdateEmployeesResponse<TProjection>>, TProjection>(r => r.Data.AddOrUpdateEmployees.Cast<TProjection>().FirstOrDefault())
                .IfSome(r => r.Error.IsNotNullOrEmpty().Do(() => _logger.ErrorAsJson(r)))
                .KeepAsSomeIf(r => r.Error.IsNullOrEmpty());
        }
    }
}
