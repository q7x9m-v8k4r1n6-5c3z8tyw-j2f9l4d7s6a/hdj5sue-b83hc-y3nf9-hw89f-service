namespace OVCMOVE.Application.Abstractions.Services;

public interface IPasswordHasher
{
    bool Verify(string password, string passwordHash);
}
