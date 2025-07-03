using System;
using System.Threading.Tasks;
using GitHubDiscordNotifier.Common.Data;
using GitHubDiscordNotifier.Common.DTOs;
using GitHubDiscordNotifier.Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GitHubDiscordNotifier.Api.Auth
{
    public class PostLogin
    {
        private readonly NotifierDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PostLogin> _logger;

        public PostLogin(NotifierDbContext dbContext, IConfiguration configuration, ILogger<PostLogin> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [FunctionName("PostLogin")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest req)
        {
            try
            {
                var requestBody = await req.ReadAsStringAsync();
                var loginRequest = JsonConvert.DeserializeObject<LoginRequestDto>(requestBody);

                if (loginRequest == null)
                {
                    return new BadRequestObjectResult(new { error = "Invalid request body" });
                }

                // Find user by email
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

                if (user == null || !PasswordHelper.VerifyPassword(loginRequest.Password, user.PasswordHash))
                {
                    return new UnauthorizedObjectResult(new { error = "Invalid email or password" });
                }

                // Generate tokens
                var jwtHelper = new JwtHelper(_configuration["JwtSecret"]);
                var accessToken = jwtHelper.GenerateAccessToken(user);
                var refreshToken = jwtHelper.GenerateRefreshToken();

                // Update user with refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                user.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                    RefreshTokenExpiry = user.RefreshTokenExpiry.Value,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        GitHubId = user.GitHubId,
                        GitHubUsername = user.GitHubUsername,
                        AvatarUrl = user.AvatarUrl,
                        CreatedAt = user.CreatedAt
                    }
                };

                _logger.LogInformation($"User logged in successfully: {user.Email}");
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return new ObjectResult(new { error = "Internal server error" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}