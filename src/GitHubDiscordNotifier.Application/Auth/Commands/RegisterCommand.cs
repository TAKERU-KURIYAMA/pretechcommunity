using FluentValidation;
using GitHubDiscordNotifier.Application.Auth.DTOs;
using GitHubDiscordNotifier.Application.Common.Interfaces;
using GitHubDiscordNotifier.Domain.Entities;
using GitHubDiscordNotifier.Domain.Interfaces;
using MediatR;

namespace GitHubDiscordNotifier.Application.Auth.Commands;

public class RegisterCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        if (await _unitOfWork.Users.ExistsAsync(request.Email))
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RefreshToken = _jwtService.GenerateRefreshToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CompleteAsync();

        // Generate tokens
        var accessToken = _jwtService.GenerateToken(user.Id, user.Email);

        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(user.Email, user.Email);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                GitHubUsername = user.GitHubUsername,
                AvatarUrl = user.AvatarUrl
            }
        };
    }
}