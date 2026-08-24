using System.Text.Json;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;

namespace OVCMOVE.Test.Application;

public sealed class WorkflowContractMappingTests
{
    [Fact]
    public void Execute_request_uses_safe_defaults_when_optional_payload_is_omitted()
    {
        var command = new WorkflowContract.ExecuteWorkflowRequest
        {
            Variables = null
        }.ToCommand(Guid.NewGuid());

        Assert.Empty(command.Input.Variables);
        Assert.Equal(JsonValueKind.Object, command.Input.Payload.ValueKind);
    }
}
