using GitHubDiscordNotifier.Api.Health;
using GitHubDiscordNotifier.Common.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Threading.Tasks;
using Xunit;

namespace GitHubDiscordNotifier.Tests
{
    public class HealthTests
    {
        [Fact]
        public async Task GetHealth_ReturnsHealthyStatus_WhenServicesAreHealthy()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NotifierDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            
            using var context = new NotifierDbContext(options);
            await context.Database.EnsureCreatedAsync();
            
            var mockRedis = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
            
            var mockLogger = new Mock<ILogger<GetHealth>>();
            
            var function = new GetHealth(context, mockRedis.Object, mockLogger.Object);
            var request = new Mock<HttpRequest>();
            
            // Act
            var result = await function.Run(request.Object);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}