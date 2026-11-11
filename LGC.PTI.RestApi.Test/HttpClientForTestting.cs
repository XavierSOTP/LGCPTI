using LGC.PTI.Refit.Authentication;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace LGC.PTI.RestApi.Test
{
    /// <summary>
    /// </summary>
    internal sealed class HttpClientForTesting
    {
        #region Public Methods

        public async Task<TokenResponse> SendTokenRequest()
        {
            var client = new HttpClient();
            // Set the basic authentication header
            var encriptedClientDetails = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            var authValue = new AuthenticationHeaderValue("Basic", encriptedClientDetails);
            client.DefaultRequestHeaders.Authorization = authValue;
            // Set the request's content
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            // Send the request and retrieve the response
            var response = await client.SendAsync(request);
            // Deserialize the response
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
            // Print the access token

            return tokenResponse;
        }

        #endregion Public Methods

        #region Private Fields

        // Set the client ID and secret for the application
        private string clientId = "QSL - PTI";

        private string clientSecret = "rCrr19";

        // Set the URL for the OAuth 2.0 token endpoint
        private string tokenUrl = "https://account.iqtechportal.com/connect/token";

        #endregion Private Fields

        // Create a new HttpClient to send the request

        //public async  Task<TokenResponse> SendTokenRequest(AuthenticationHeaderValue authValue, StringContent content , HttpMethod method, string url)
        //{
        //    var client = new HttpClient();
        //    // Set the basic authentication header
        //    //var encriptedClientDetails = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        //    //var authValue = new AuthenticationHeaderValue("Basic", encriptedClientDetails );
        //    client.DefaultRequestHeaders.Authorization = authValue;
        //    // Set the request's content
        //    var request = new HttpRequestMessage(method,url);
        //    request.Content = content;
        //    // Send the request and retrieve the response
        //    var response = await client.SendAsync(request);
        //    // Deserialize the response
        //     var responseContent = await response.Content.ReadAsStringAsync();
        //    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
        //    // Print the access token

        //    return tokenResponse;
        //}
    }
}