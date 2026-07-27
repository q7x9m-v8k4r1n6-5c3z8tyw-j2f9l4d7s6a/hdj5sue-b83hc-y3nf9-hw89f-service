using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Auth.Query.GetMe;

public class GetMeQueryHandler : IRequestHandler<GetMeQuery, GetMeResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserAccessRepository _userAccessRepository;

    public GetMeQueryHandler(
        IUserRepository userRepository,
        IUserAccessRepository userAccessRepository)
    {
        _userRepository = userRepository;
        _userAccessRepository = userAccessRepository;
    }

    /// <summary>Returns the authenticated user's profile and effective access.</summary>
    public async Task<GetMeResult> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
            throw new UnauthorizedAccessException("Tài khoản không tồn tại");

        var accessProfile = await _userAccessRepository.GetAccessProfileAsync(user.Id, cancellationToken);

        return new GetMeResult(
            user.Id,
            user.LinkedEmail,
            accessProfile.Roles,
            accessProfile.Permissions,
            accessProfile.Access,
            user.DisplayName,
            user.Status
        );
    }
}
