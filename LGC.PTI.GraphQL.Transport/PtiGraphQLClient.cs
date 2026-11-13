using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    /// <summary>Standalone GraphQL transport. Does not reference the legacy GraphQL project or the REST adapter.</summary>
    public sealed class PtiGraphQLClient : IPtiGraphQLClient
    {
        private const string EmployeeFields = "number pin firstName lastName departmentNumber active deleted type unrestrictedUsage status error";
        private const string LoginMutation = "mutation Login($email: String!, $password: String!) { login(email: $email, password: $password) { token } }";
        private readonly HttpClient _http;
        private readonly string _email;
        private readonly string _password;
        private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);
        private readonly Func<DateTimeOffset> _clock;
        private string _token;
        private DateTimeOffset _expiresAt;

        public PtiGraphQLClient(HttpClient httpClient, string email, string password, Func<DateTimeOffset> utcNow = null)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));
            if (_http.BaseAddress == null) throw new ArgumentException("HttpClient.BaseAddress must point to the GraphQL endpoint.", nameof(httpClient));
            _email = email;
            _password = password;
            _clock = utcNow ?? (() => DateTimeOffset.UtcNow);
        }

        public Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default)
        {
            return ExecuteEmployeesAsync("query GetEmployees { getEmployees { " + EmployeeFields + " } }", null, "getEmployees", cancellationToken);
        }

        public Task<IReadOnlyList<Employee>> AddOrUpdateEmployeesAsync(IReadOnlyCollection<EmployeeInput> employees, CancellationToken cancellationToken = default)
        {
            if (employees == null || employees.Count == 0) throw new ArgumentException("At least one employee is required.", nameof(employees));
            foreach (var employee in employees)
            {
                if (employee == null || string.IsNullOrWhiteSpace(employee.Number) || string.IsNullOrWhiteSpace(employee.Pin) ||
                    string.IsNullOrWhiteSpace(employee.FirstName) || string.IsNullOrWhiteSpace(employee.LastName) ||
                    string.IsNullOrWhiteSpace(employee.DepartmentNumber))
                    throw new ArgumentException("GraphQL requires number, pin, firstName, lastName, and departmentNumber.", nameof(employees));
            }

            const string mutation = "mutation UpsertEmployees($employees: [EmployeeInput]) { addOrUpdateEmployees(employees: $employees) { " + EmployeeFields + " } }";
            return ExecuteEmployeesAsync(mutation, new { employees }, "addOrUpdateEmployees", cancellationToken);
        }

        private async Task<IReadOnlyList<Employee>> ExecuteEmployeesAsync(string query, object variables, string field, CancellationToken ct)
        {
            var token = await GetTokenAsync(ct).ConfigureAwait(false);
            JObject response = await SendAsync(query, variables, token, ct).ConfigureAwait(false);
            if (IsAuthenticationError(response))
            {
                await InvalidateTokenAsync(token, ct).ConfigureAwait(false);
                token = await GetTokenAsync(ct).ConfigureAwait(false);
                response = await SendAsync(query, variables, token, ct).ConfigureAwait(false);
            }

            ThrowGraphQLErrors(response);
            var data = response["data"] as JObject;
            var node = data?[field];
            if (!(node is JArray employees)) throw new GraphQLRequestException("GraphQL response did not contain a valid " + field + " array.");
            var parsed = employees.ToObject<List<Employee>>();
            if (parsed == null || parsed.Any(x => x == null || !string.IsNullOrWhiteSpace(x.Error) ||
                (!string.IsNullOrEmpty(x.Status) && x.Status != "new" && x.Status != "updated")))
                throw new GraphQLRequestException("PTI returned an employee with an error or unrecognized status.");
            return parsed;
        }

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (TokenIsValid()) return _token;
            await _loginLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (TokenIsValid()) return _token;
                var result = await SendAsync(LoginMutation, new { email = _email, password = _password }, null, ct).ConfigureAwait(false);
                ThrowGraphQLErrors(result);
                var token = (string)result["data"]?["login"]?["token"];
                if (string.IsNullOrWhiteSpace(token)) throw new GraphQLRequestException("GraphQL login did not return a token.");
                var expiry = ReadJwtExpiry(token);
                if (expiry <= _clock()) throw new GraphQLRequestException("GraphQL login returned an expired JWT.");
                _token = token;
                _expiresAt = expiry;
                return _token;
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

        private async Task<JObject> SendAsync(string query, object variables, string token, CancellationToken ct)
        {
            var json = JsonConvert.SerializeObject(new { query, variables });
            using (var request = new HttpRequestMessage(HttpMethod.Post, _http.BaseAddress))
            using (var body = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                request.Content = body;
                if (!string.IsNullOrEmpty(token)) request.Headers.TryAddWithoutValidation("Authorization", token);
                using (var response = await _http.SendAsync(request, ct).ConfigureAwait(false))
                {
                    var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JObject document;
                    try { document = JObject.Parse(responseText); }
                    catch (JsonException e) { throw new GraphQLRequestException("PTI returned an invalid JSON response.", e); }
                    if (!response.IsSuccessStatusCode && !IsAuthenticationError(document))
                        throw new GraphQLRequestException("PTI HTTP error: " + (int)response.StatusCode, response.StatusCode);
                    return document;
                }
            }
        }

        private static bool IsAuthenticationError(JObject response)
        {
            var errors = response["errors"] as JArray;
            return errors != null && errors.Any(error =>
            {
                var message = (string)error["message"] ?? "";
                return message.IndexOf("Invalid authentication token", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       message.IndexOf("Firebase ID token", StringComparison.OrdinalIgnoreCase) >= 0;
            });
        }

        private static void ThrowGraphQLErrors(JObject response)
        {
            var errors = response["errors"] as JArray;
            if (errors != null && errors.Count > 0)
                throw new GraphQLRequestException("PTI GraphQL error: " + string.Join("; ", errors.Select(e => (string)e["message"] ?? "Unknown error")));
        }

        private static DateTimeOffset ReadJwtExpiry(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) throw new FormatException();
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
                var claims = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                var unixSeconds = (long?)claims["exp"];
                if (unixSeconds == null) throw new FormatException();
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value);
            }
            catch (Exception e) when (e is FormatException || e is JsonException || e is ArgumentOutOfRangeException)
            {
                throw new GraphQLRequestException("GraphQL login returned a JWT without a valid expiration.", e);
            }
        }
    }
}
