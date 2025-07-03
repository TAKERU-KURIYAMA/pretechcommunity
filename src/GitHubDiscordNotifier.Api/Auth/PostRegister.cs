using System;
using System.Linq;
using System.Threading.Tasks;
using GitHubDiscordNotifier.Common.Data;
using GitHubDiscordNotifier.Common.DTOs;
using GitHubDiscordNotifier.Common.Models;
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
    public class PostRegister
    {
        private readonly NotifierDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PostRegister> _logger;

        public PostRegister(NotifierDbContext dbContext, IConfiguration configuration, ILogger<PostRegister> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [FunctionName("PostRegister")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequest req)
        {
            try
            {
                var requestBody = await req.ReadAsStringAsync();
                var registerRequest = JsonConvert.DeserializeObject<RegisterRequestDto>(requestBody);

                if (registerRequest == null)
                {
                    return new BadRequestObjectResult(new { error = "Invalid request body" });
                }

                // Validate password
                if (!PasswordHelper.IsValidPassword(registerRequest.Password))
                {
                    return new BadRequestObjectResult(new 
                    { 
                        error = "Password must be at least 8 characters and contain uppercase, lowercase, digit, and special character" 
                    });
                }

                // Check if user already exists
                var existingUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == registerRequest.Email);

                if (existingUser != null)
                {
                    return new ConflictObjectResult(new { error = "User already exists" });
                }

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = registerRequest.Email,
                    PasswordHash = PasswordHelper.HashPassword(registerRequest.Password),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();

                // Generate tokens
                var jwtHelper = new JwtHelper(_configuration["JwtSecret"]);
                var accessToken = jwtHelper.GenerateAccessToken(user);
                var refreshToken = jwtHelper.GenerateRefreshToken();

                // Update user with refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
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

                _logger.LogInformation($"User registered successfully: {user.Email}");
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return new ObjectResult(new { error = "Internal server error" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}