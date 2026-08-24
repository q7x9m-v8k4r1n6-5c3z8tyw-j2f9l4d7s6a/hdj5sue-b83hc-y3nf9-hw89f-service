using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class WorkflowRetryPolicy(ITransientErrorDetector transientErrorDetector)
{
    public const int MaximumAttempts = 3;

    public async Task<T> ExecuteAsync<T>(
        Func<int, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                return await operation(attempt, cancellationToken);
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException &&
                exception is not ApplicationCommitOutcomeUnknownException &&
                transientErrorDetector.IsTransient(exception))
            {
                if (attempt == MaximumAttempts)
                {
                    throw new ApplicationServiceUnavailableException(
                        "Hệ thống chưa thể hoàn tất thao tác. Dữ liệu hiện tại vẫn được giữ nguyên, vui lòng thử lại.",
                        exception);
                }

                await Task.Delay(RetryDelay(attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException("Workflow retry policy reached an invalid state.");
    }

    private static TimeSpan RetryDelay(int attempt) =>
        TimeSpan.FromMilliseconds(attempt * 150);
}
