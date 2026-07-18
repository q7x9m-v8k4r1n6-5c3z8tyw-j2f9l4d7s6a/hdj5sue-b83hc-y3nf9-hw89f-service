using AutoMapper;

using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Application.Features.Auth.Command.Logout;
using OVCMOVE.Application.Features.Auth.Command.Refresh;
using OVCMOVE.Application.Features.Auth.Command.GoogleLogin;
using OVCMOVE.Application.Features.Auth.Queries.GetMe;

namespace OVCMOVE.Api.Mapping;

public class AuthProfile : Profile
{
    public AuthProfile()
    {
        // --- REQUEST ---
        CreateMap<AuthContract.LoginRequest, LoginCommand>();
        CreateMap<AuthContract.LogoutRequest, LogoutCommand>();
        CreateMap<AuthContract.GoogleLoginRequest, GoogleLoginCommand>();
        CreateMap<AuthContract.RefreshTokenRequest, RefreshTokenCommand>();

        // --- RESPONSE ---
        CreateMap<LoginResult, AuthContract.LoginResponse>();
        CreateMap<GetMeResult, AuthContract.MeResponse>();
    }
}