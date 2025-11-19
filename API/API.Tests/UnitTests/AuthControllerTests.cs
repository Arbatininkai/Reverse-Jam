 using Xunit;
using Moq;
using API.Controllers;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace API.Tests.UnitTests
{
    public class AuthControllerTests
    {
        // ---------------------- Helper Methods ----------------------

        private AppDbContext GetMockDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "UnitTestDB")
                .Options;

            return new AppDbContext(options);
        }

        private void MockHttpContextWithUser(AuthController controller, string email)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new Claim[] { new Claim(ClaimTypes.Email, email) }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        // ---------------------- Unit Tests ----------------------

        [Fact]
        public async Task GoogleSignIn_InvalidToken_ReturnsBadRequest()
        {
            // ARRANGE
            var context = GetMockDbContext();
            var controller = new AuthController(context);

            // ACT
            var result = await controller.GoogleSignIn("invalid_token");

            // ASSERT
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetUserInfo_UserNotFound_ReturnsNotFound()
        {
            // ARRANGE
            var context = GetMockDbContext();
            var controller = new AuthController(context);
            MockHttpContextWithUser(controller, "missing@user.com");

            // ACT
            var result = await controller.GetUserInfo();

            // ASSERT
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetUserInfo_ReturnsUser_WhenExists()
        {
            // ARRANGE
            var context = GetMockDbContext();
            await context.Users.AddAsync(new User { Email = "Jonas@gmail.com", Name = "Jonas" });
            await context.SaveChangesAsync();

            var controller = new AuthController(context);
            MockHttpContextWithUser(controller, "Jonas@gmail.com");

            // ACT
            var result = await controller.GetUserInfo();

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result);
            var user = Assert.IsType<User>(okResult.Value);
            Assert.Equal("Jonas@gmail.com", user.Email);
        }

        [Fact]
        public void Me_ReturnsUserIdAndEmail()
        {
            // ARRANGE
            var context = GetMockDbContext();
            var controller = new AuthController(context);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Jonas"),
                new Claim(ClaimTypes.Email, "Jonas@gmail.com")
            };

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"))
                }
            };

            // ACT
            var result = controller.Me();

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Jonas@gmail.com", okResult.Value.ToString());
        }
    }
}
