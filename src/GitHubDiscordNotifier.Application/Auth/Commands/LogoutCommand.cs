using GitHubDiscordNotifier.Domain.Interfaces;
using MediatR;

namespace GitHubDiscordNotifier.Application.Auth.Commands;

public class LogoutCommand : IRequest
{
    public Guid UserId { get; set; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();
        }
    }
}