using FluentValidation;
using GitHubDiscordNotifier.Application.Auth.DTOs;
using GitHubDiscordNotifier.Application.Common.Interfaces;
using GitHubDiscordNotifier.Domain.Interfaces;
using MediatR;

namespace GitHubDiscordNotifier.Application.Auth.Commands;

public class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByRefreshTokenAsync(request.RefreshToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Generate new tokens
        var accessToken = _jwtService.GenerateToken(user.Id, user.Email);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Update refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.CompleteAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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