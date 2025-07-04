using GitHubDiscordNotifier.Application.Auth.DTOs;
using GitHubDiscordNotifier.Domain.Interfaces;
using MediatR;

namespace GitHubDiscordNotifier.Application.Auth.Queries;

public class GetCurrentUserQuery : IRequest<UserDto>
{
    public Guid UserId { get; set; }
}

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            GitHubUsername = user.GitHubUsername,
            AvatarUrl = user.AvatarUrl
        };
    }
}