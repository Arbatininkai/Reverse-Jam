using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace API.Tests.IntegrationTests
{
    public class AuthControllerIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Me_ReturnsUnauthorized_IfNoToken()
        {
            // ARRANGE
            // (TestServer created in constructor)

            // ACT
            var response = await _client.GetAsync("/api/auth/me");

            // ASSERT
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GoogleSignIn_InvalidToken_ReturnsBadRequest()
        {
            // ARRANGE
            var body = JsonConvert.SerializeObject("invalid_token");
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            // ACT
            var response = await _client.PostAsync("/api/auth/google-signin", content);

            // ASSERT
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetUserInfo_ReturnsUnauthorized_IfNoToken()
        {
            // ARRANGE
            // (TestServer created in constructor)

            // ACT
            var response = await _client.GetAsync("/api/auth/userinfo");

            // ASSERT
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}