namespace OVCMOVE.Application.Abstractions.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}