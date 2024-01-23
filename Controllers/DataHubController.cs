using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace OKPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataHubController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public DataHubController()
        {
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            DotNetEnv.Env.Load();
            string client_secret = Environment.GetEnvironmentVariable("client_secret");
            string username = Environment.GetEnvironmentVariable("username");
            string password = Environment.GetEnvironmentVariable("password");

            try
            {
                var authBody = new NameValueCollection
                {
                    {"client_id", "datahub-api"},
                    {"client_secret", client_secret},
                    {"grant_type", "password"},
                    {"username", username},
                    {"password", password}
                };

                var authRes = await _httpClient.PostAsync("https://iam-datahub.visitfinland.com/auth/realms/Datahub/protocol/openid-connect/token",
    new FormUrlEncodedContent(authBody.Cast<string>().Select(key => new KeyValuePair<string, string>(key, authBody[key]))));

                authRes.EnsureSuccessStatusCode();
                var authResultJson = await authRes.Content.ReadAsStringAsync();
                var authResult = JsonSerializer.Deserialize<AuthResult>(authResultJson);
                Console.WriteLine($"{authResult.access_token} GGGGGGEGEGEGEG");

                var graphqlQuery = new
                {
                    query = @"
                        query GetGroupedProducts {
                            groupedProducts(args: {publishing_id: ""a5d8d6e4-869c-4de6-abf8-5cd5d288447a""}) {
                                id
                                productInformations(where: { language: { _eq: fi } }) {
                                    name
                                    description
                                }
                            }
                        }"
                };

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.access_token);
                var graphqlRes = await _httpClient.PostAsync(
                                                        "https://api-datahub.visitfinland.com/graphql/v1/graphql",
                                                        new StringContent(JsonSerializer.Serialize(graphqlQuery), Encoding.UTF8, "application/json"));

                graphqlRes.EnsureSuccessStatusCode();
                var queryResultJson = await graphqlRes.Content.ReadAsStringAsync();
                var queryResult = JsonSerializer.Deserialize<object>(queryResultJson);

                return Ok(queryResult);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error2: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        private class AuthResult
        {
            public string access_token { get; set; }
        }
    }
}