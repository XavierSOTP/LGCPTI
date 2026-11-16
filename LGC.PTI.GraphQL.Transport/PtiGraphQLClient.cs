using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace LGC.PTI.GraphQL.Transport
{
    public sealed class EmployeeInput
    {
        [JsonProperty("number")] public string Number { get; set; }
        [JsonProperty("pin")] public string Pin { get; set; }
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonProperty("departmentNumber")] public string DepartmentNumber { get; set; }
        [JsonProperty("active")] public bool Active { get; set; }
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)] public string Type { get; set; }
        [JsonProperty("unrestrictedUsage", NullValueHandling = NullValueHandling.Ignore)] public bool? UnrestrictedUsage { get; set; }
    }

    public sealed class Employee
    {
        [JsonProperty("number")] public string Number { get; set; }
        [JsonProperty("pin")] public string Pin { get; set; }
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonProperty("departmentNumber")] public string DepartmentNumber { get; set; }
        [JsonProperty("active")] public bool Active { get; set; }
        [JsonProperty("deleted")] public bool Deleted { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("unrestrictedUsage")] public bool? UnrestrictedUsage { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("error")] public string Error { get; set; }
    }

    /// <summary>Typed GraphQL transport. Authentication remains PTI-specific; GraphQL parsing is handled by NuGet.</summary>
    public sealed class PtiGraphQLClient : IPtiGraphQLClient
    {
        private const string EmployeeFields = "number pin firstName lastName departmentNumber active deleted type unrestrictedUsage status error";
        private const string LoginMutation = "mutation Login($email: String!, $password: String!) { login(email: $email, password: $password) { token } }";
        private const string EmployeesQuery = "query GetEmployees { getEmployees { " + EmployeeFields + " } }";
        private const string UpsertMutation = "mutation UpsertEmployees($employees: [EmployeeInput]) { addOrUpdateEmployees(employees: $employees) { " + EmployeeFields + " } }";
        private readonly GraphQLHttpClient _graphql;
        private readonly string _email;
        private readonly string _password;
        private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);
        private readonly Func<DateTimeOffset> _clock;
        private string _token;
        private DateTimeOffset _expiresAt;

        public PtiGraphQLClient(HttpClient httpClient, string email, string password, Func<DateTimeOffset> utcNow = null)
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            if (httpClient.BaseAddress == null) throw new ArgumentException("HttpClient.BaseAddress must point to the GraphQL endpoint.", nameof(httpClient));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));

            _email = email;
            _password = password;
            _clock = utcNow ?? (() => DateTimeOffset.UtcNow);
            var options = new GraphQLHttpClientOptions
            {
                EndPoint = httpClient.BaseAddress,
                IsValidResponseToDeserialize = response => GraphQLHttpClientOptions.DefaultIsValidResponseToDeserialize(response) ||
                    (response.StatusCode == HttpStatusCode.Unauthorized && IsJsonResponse(response))
            };
            _graphql = new GraphQLHttpClient(options, new NewtonsoftJsonSerializer(), httpClient);
        }

        public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync<EmployeesResult>(EmployeesQuery, null, false, cancellationToken).ConfigureAwait(false);
            return ValidateEmployees(response.Data?.GetEmployees, "getEmployees");
        }

        public async Task<IReadOnlyList<Employee>> AddOrUpdateEmployeesAsync(IReadOnlyCollection<EmployeeInput> employees, CancellationToken cancellationToken = default)
        {
            if (employees == null || employees.Count == 0) throw new ArgumentException("At least one employee is required.", nameof(employees));
            if (employees.Any(employee => employee == null || string.IsNullOrWhiteSpace(employee.Number) ||
                string.IsNullOrWhiteSpace(employee.Pin) || string.IsNullOrWhiteSpace(employee.FirstName) ||
                string.IsNullOrWhiteSpace(employee.LastName) || string.IsNullOrWhiteSpace(employee.DepartmentNumber)))
                throw new ArgumentException("GraphQL requires number, pin, firstName, lastName, and departmentNumber.", nameof(employees));

            var response = await ExecuteAsync<UpsertResult>(UpsertMutation, new { employees }, true, cancellationToken).ConfigureAwait(false);
            return ValidateEmployees(response.Data?.AddOrUpdateEmployees, "addOrUpdateEmployees");
        }

        private async Task<GraphQLResponse<T>> ExecuteAsync<T>(string query, object variables, bool isMutation, CancellationToken ct)
        {
            var token = await GetTokenAsync(ct).ConfigureAwait(false);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                GraphQLResponse<T> response;
                try
                {
                    response = await SendAsync<T>(query, variables, token, isMutation, ct).ConfigureAwait(false);
                }
                catch (GraphQLRequestException ex) when (attempt == 0 && ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await InvalidateTokenAsync(token, ct).ConfigureAwait(false);
                    token = await GetTokenAsync(ct).ConfigureAwait(false);
                    continue;
                }

                if (attempt == 0 && IsAuthenticationError(response))
                {
                    await InvalidateTokenAsync(token, ct).ConfigureAwait(false);
                    token = await GetTokenAsync(ct).ConfigureAwait(false);
                    continue;
                }

                ThrowGraphQLErrors(response);
                return response;
            }

            throw new GraphQLRequestException("PTI GraphQL authentication retry exhausted.");
        }

        private async Task<GraphQLResponse<T>> SendAsync<T>(string query, object variables, string token, bool isMutation, CancellationToken ct)
        {
            GraphQLRequest request = new GraphQLRequest { Query = query, Variables = variables };
            if (!string.IsNullOrEmpty(token)) request = new AuthorizedGraphQLRequest(request, token);

            try
            {
                return isMutation
                    ? await _graphql.SendMutationAsync<T>(request, ct).ConfigureAwait(false)
                    : await _graphql.SendQueryAsync<T>(request, ct).ConfigureAwait(false);
            }
            catch (GraphQLHttpRequestException ex)
            {
                throw new GraphQLRequestException("PTI HTTP error: " + (int)ex.StatusCode, ex.StatusCode);
            }
            catch (JsonException ex)
            {
                throw new GraphQLRequestException("PTI returned an invalid JSON response.", ex);
            }
        }

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (TokenIsValid()) return _token;
            await _loginLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (TokenIsValid()) return _token;

                var result = await SendAsync<LoginResult>(LoginMutation, new { email = _email, password = _password }, null, true, ct).ConfigureAwait(false);
                ThrowGraphQLErrors(result);

                var token = result.Data?.Login?.Token;
                if (string.IsNullOrWhiteSpace(token)) throw new GraphQLRequestException("GraphQL login did not return a token.");

                var expiresAt = ReadJwtExpiry(token);
                if (expiresAt <= _clock()) throw new GraphQLRequestException("GraphQL login returned an expired JWT.");

                _token = token;
                _expiresAt = expiresAt;
                return token;
            }
            finally { _loginLock.Release(); }
        }

        private bool TokenIsValid() => !string.IsNullOrEmpty(_token) && _clock().AddMinutes(5) < _expiresAt;

        private async Task InvalidateTokenAsync(string rejectedToken, CancellationToken ct)
        {
            await _loginLock.WaitAsync(ct).ConfigureAwait(false);
            try { if (_token == rejectedToken) { _token = null; _expiresAt = default; } }
            finally { _loginLock.Release(); }
        }

        private static IReadOnlyList<Employee> ValidateEmployees(List<Employee> employees, string field)
        {
            if (employees == null) throw new GraphQLRequestException("GraphQL response did not contain a valid " + field + " array.");
            if (employees.Any(employee => employee == null || !string.IsNullOrWhiteSpace(employee.Error) ||
                (!string.IsNullOrEmpty(employee.Status) && employee.Status != "new" && employee.Status != "updated")))
                throw new GraphQLRequestException("PTI returned an employee with an error or unrecognized status.");
            return employees;
        }

        private static bool IsJsonResponse(HttpResponseMessage response)
        {
            var mediaType = response.Content?.Headers.ContentType?.MediaType;
            return mediaType == "application/json" || mediaType == "application/graphql-response+json" || mediaType == "application/graphql+json";
        }

        private static bool IsAuthenticationError<T>(GraphQLResponse<T> response)
        {
            if (response is IGraphQLHttpResponse http && http.StatusCode == HttpStatusCode.Unauthorized) return true;
            return response?.Errors?.Any(error =>
                (error?.Message ?? "").IndexOf("Invalid authentication token", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (error?.Message ?? "").IndexOf("Firebase ID token", StringComparison.OrdinalIgnoreCase) >= 0) == true;
        }

        private static void ThrowGraphQLErrors<T>(GraphQLResponse<T> response)
        {
            if (response == null) throw new GraphQLRequestException("PTI returned an empty GraphQL response.");
            if (response.Errors?.Length > 0)
                throw new GraphQLRequestException("PTI GraphQL error: " + string.Join("; ", response.Errors.Select(e => e?.Message ?? "Unknown error")));
        }

        private static DateTimeOffset ReadJwtExpiry(string token)
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                if (jwt.ValidTo == DateTime.MinValue) throw new FormatException("JWT does not contain exp.");
                return new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException || ex is FormatException)
            {
                throw new GraphQLRequestException("GraphQL login returned a JWT without a valid expiration.", ex);
            }
        }

        private sealed class AuthorizedGraphQLRequest : GraphQLHttpRequest
        {
            private readonly string _jwt;

            public AuthorizedGraphQLRequest(GraphQLRequest request, string jwt) : base(request) => _jwt = jwt;

            public override HttpRequestMessage ToHttpRequestMessage(GraphQLHttpClientOptions options, IGraphQLJsonSerializer serializer)
            {
                var request = base.ToHttpRequestMessage(options, serializer);
                request.Headers.TryAddWithoutValidation("Authorization", _jwt); // PTI expects the raw Firebase ID token.
                return request;
            }
        }

        private sealed class LoginResult
        {
            [JsonProperty("login")] public LoginToken Login { get; set; }
        }

        private sealed class LoginToken
        {
            [JsonProperty("token")] public string Token { get; set; }
        }

        private sealed class EmployeesResult
        {
            [JsonProperty("getEmployees")] public List<Employee> GetEmployees { get; set; }
        }

        private sealed class UpsertResult
        {
            [JsonProperty("addOrUpdateEmployees")] public List<Employee> AddOrUpdateEmployees { get; set; }
        }
    }
}
