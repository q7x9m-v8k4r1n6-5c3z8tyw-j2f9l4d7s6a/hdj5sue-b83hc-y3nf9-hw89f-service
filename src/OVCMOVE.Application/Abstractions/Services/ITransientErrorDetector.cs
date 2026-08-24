namespace OVCMOVE.Application.Abstractions.Services;

public interface ITransientErrorDetector
{
    bool IsTransient(Exception exception);
}
