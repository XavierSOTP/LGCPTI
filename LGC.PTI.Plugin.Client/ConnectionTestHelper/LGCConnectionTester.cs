using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LGC.PTI.Plugin.Client;

public sealed class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    // Kept only in Plugin Client memory and never persisted in Genetec configuration.
    internal string Token { get; set; } = string.Empty;
}

/// <summary>A department returned from the PTI GraphQL endpoint.</summary>
public sealed class PtiDepartment
{
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class DepartmentsResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool AuthenticationFailed { get; set; }
    public IReadOnlyList<PtiDepartment> Departments { get; set; } = Array.Empty<PtiDepartment>();
}

/// <summary>Tests PTI credentials through the GraphQL login mutation, not REST OAuth.</summary>
public static class LGCConnectionTester
{
    private static readonly HttpClient Client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static async Task<ConnectionTestResult> TestAsync(
        string endpoint, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
            return Fail("Enter the GraphQL URL, email, and password before testing.");

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
            return Fail("GraphQL URL must be a valid HTTPS URL.");

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                query = "mutation Login($email: String!, $password: String!) { login(email: $email, password: $password) { token } }",
                variables = new { email, password }
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            using var response = await Client.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return Fail($"GraphQL login failed (HTTP {(int)response.StatusCode}). Check endpoint and credentials.");

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                return Fail("PTI rejected the GraphQL login. Check the email and password.");

            if (root.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("login", out var login) &&
                login.ValueKind == JsonValueKind.Object &&
                login.TryGetProperty("token", out var token) &&
                token.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(token.GetString()))
                return Ok("GraphQL connection successful. Login token obtained.", token.GetString());

            return Fail("GraphQL login did not return a token.");
        }
        catch (TaskCanceledException)
        {
            return Fail("Connection timed out (15 seconds). Check endpoint and network.");
        }
        catch (HttpRequestException)
        {
            return Fail("Unable to reach the GraphQL endpoint. Check URL and network.");
        }
        catch (System.Text.Json.JsonException)
        {
            return Fail("PTI returned an invalid GraphQL response.");
        }
        catch (Exception)
        {
            return Fail("Unexpected error while testing the GraphQL connection.");
        }
    }

    /// <summary>
    /// Gets PTI department identifiers and names with the token obtained by Test Connection.
    /// This operation is read-only and does not store credentials or tokens on disk.
    /// </summary>
    public static Task<DepartmentsResult> GetDepartmentsAsync(string endpoint, string token) =>
        GetDepartmentsAsync(endpoint, token, Client);

    // Allows isolated HTTP testing without live PTI credentials or endpoint calls.
    internal static async Task<DepartmentsResult> GetDepartmentsAsync(string endpoint, string token, HttpClient client)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
            return DepartmentsFail("Test Connection successfully before retrieving departments.", true);

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                query = "query GetDepartments { getDepartments { number name } }"
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            // PTI expects the Firebase ID token directly, without a Bearer prefix.
            request.Headers.TryAddWithoutValidation("Authorization", token);
            using var response = await client.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
                return DepartmentsFail("The PTI session is no longer authorized. Test Connection again.", true);

            if (!response.IsSuccessStatusCode)
                return DepartmentsFail($"Unable to retrieve departments (HTTP {(int)response.StatusCode}).");

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            {
                foreach (var error in errors.EnumerateArray())
                {
                    if (error.ValueKind == JsonValueKind.Object &&
                        error.TryGetProperty("message", out var message) &&
                        message.ValueKind == JsonValueKind.String)
                    {
                        var detail = message.GetString() ?? string.Empty;
                        if (detail.IndexOf("Invalid authentication token", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            detail.IndexOf("Firebase ID token", StringComparison.OrdinalIgnoreCase) >= 0)
                            return DepartmentsFail("The PTI session has expired. Test Connection again.", true);
                    }
                }

                return DepartmentsFail("PTI rejected the getDepartments query. Check your account permissions.");
            }

            if (!root.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Object ||
                !data.TryGetProperty("getDepartments", out var departmentList) ||
                departmentList.ValueKind != JsonValueKind.Array)
                return DepartmentsFail("PTI did not return a valid getDepartments list.");

            var departments = new List<PtiDepartment>();
            foreach (var entry in departmentList.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object ||
                    !entry.TryGetProperty("number", out var numberElement) ||
                    !entry.TryGetProperty("name", out var nameElement))
                    return DepartmentsFail("PTI returned a department without number or name.");

                var number = numberElement.ValueKind == JsonValueKind.String
                    ? numberElement.GetString()
                    : numberElement.ValueKind == JsonValueKind.Number ? numberElement.ToString() : null;
                var name = nameElement.ValueKind == JsonValueKind.String ? nameElement.GetString() : null;

                if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(name))
                    return DepartmentsFail("PTI returned a department with an invalid number or name.");

                departments.Add(new PtiDepartment { Number = number, Name = name });
            }

            return new DepartmentsResult
            {
                Success = true,
                Departments = departments,
                Message = departments.Count == 0
                    ? "PTI returned no departments."
                    : $"Loaded {departments.Count} departments from PTI."
            };
        }
        catch (TaskCanceledException)
        {
            return DepartmentsFail("Department request timed out (15 seconds).");
        }
        catch (HttpRequestException)
        {
            return DepartmentsFail("Could not reach PTI to retrieve departments.");
        }
        catch (System.Text.Json.JsonException)
        {
            return DepartmentsFail("PTI returned an invalid departments response.");
        }
        catch (Exception)
        {
            return DepartmentsFail("Unexpected error while retrieving departments.");
        }
    }

    private static ConnectionTestResult Ok(string message, string token) =>
        new ConnectionTestResult { Success = true, Message = message, Token = token };

    private static ConnectionTestResult Fail(string message) =>
        new ConnectionTestResult { Success = false, Message = message };

    private static DepartmentsResult DepartmentsFail(string message, bool authenticationFailed = false) =>
        new DepartmentsResult { Success = false, Message = message, AuthenticationFailed = authenticationFailed };
}
