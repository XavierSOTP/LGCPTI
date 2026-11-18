using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LGC.PTI.Plugin.Client;

public sealed class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
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
            var payload = JsonSerializer.Serialize(new
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
                return Ok("GraphQL connection successful. Login token obtained.");

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
        catch (JsonException)
        {
            return Fail("PTI returned an invalid GraphQL response.");
        }
        catch (Exception)
        {
            return Fail("Unexpected error while testing the GraphQL connection.");
        }
    }

    private static ConnectionTestResult Ok(string message) =>
        new ConnectionTestResult { Success = true, Message = message };

    private static ConnectionTestResult Fail(string message) =>
        new ConnectionTestResult { Success = false, Message = message };
}
