using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Controllers.v1;

namespace OVCMOVE.Test.Application;

public class AuthenticatedControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void RequiredUserId_InvalidClaim_ThrowsUnauthorized(string? value)
    {
        var claims = value is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.NameIdentifier, value)];
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            }
        };

        Assert.Throws<UnauthorizedAccessException>(
            () =>
            {
                controller.ReadRequiredUserId();
            });
    }

    private sealed class TestController() : BaseController(null!)
    {
        public Guid ReadRequiredUserId() => GetRequiredCurrentUserId();
    }
}
