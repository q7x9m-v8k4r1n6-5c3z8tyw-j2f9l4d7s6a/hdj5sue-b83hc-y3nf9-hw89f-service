using OVCMOVE.Infrastructure.Services;

namespace OVCMOVE.Test.Application;

public class BcryptPasswordHasherTests
{
    [Fact]
    public void Verify_AcceptsMatchingBcryptHash()
    {
        const string password = "correct-password";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var hasher = new BcryptPasswordHasher();

        Assert.True(hasher.Verify(password, hash));
        Assert.False(hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Verify_RejectsMalformedHash()
    {
        var hasher = new BcryptPasswordHasher();

        Assert.False(hasher.Verify("password", "plaintext-value"));
    }
}
