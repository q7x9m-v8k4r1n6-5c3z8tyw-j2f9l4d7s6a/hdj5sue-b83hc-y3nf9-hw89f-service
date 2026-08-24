using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Workflows.Command;

namespace OVCMOVE.Test.Application;

public sealed class WorkflowRetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_retries_transient_failure_until_operation_succeeds()
    {
        var attempts = 0;
        var policy = new WorkflowRetryPolicy(new TransientErrorDetectorDouble());

        var result = await policy.ExecuteAsync(
            (_, _) =>
            {
                attempts++;
                if (attempts < WorkflowRetryPolicy.MaximumAttempts)
                    throw new TransientTestException();

                return Task.FromResult("completed");
            },
            CancellationToken.None);

        Assert.Equal("completed", result);
        Assert.Equal(WorkflowRetryPolicy.MaximumAttempts, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_does_not_retry_business_validation_failure()
    {
        var attempts = 0;
        var policy = new WorkflowRetryPolicy(new TransientErrorDetectorDouble());

        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            policy.ExecuteAsync<string>(
                (_, _) =>
                {
                    attempts++;
                    throw new ApplicationValidationException("invalid");
                },
                CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_returns_friendly_service_error_after_three_transient_failures()
    {
        var attempts = 0;
        var policy = new WorkflowRetryPolicy(new TransientErrorDetectorDouble());

        var exception = await Assert.ThrowsAsync<ApplicationServiceUnavailableException>(() =>
            policy.ExecuteAsync<string>(
                (_, _) =>
                {
                    attempts++;
                    throw new TransientTestException();
                },
                CancellationToken.None));

        Assert.Equal(WorkflowRetryPolicy.MaximumAttempts, attempts);
        Assert.Contains("Dữ liệu hiện tại vẫn được giữ nguyên", exception.Message);
    }

    private sealed class TransientErrorDetectorDouble : ITransientErrorDetector
    {
        public bool IsTransient(Exception exception) =>
            exception is TransientTestException;
    }

    private sealed class TransientTestException : Exception;
}
