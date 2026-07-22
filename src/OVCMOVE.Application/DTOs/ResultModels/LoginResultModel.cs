using System;

namespace OVCMOVE.Application.DTOs.ResultModels;

public class LoginResultModel
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiration { get; init; }
    public Guid UserId { get; init; }
}