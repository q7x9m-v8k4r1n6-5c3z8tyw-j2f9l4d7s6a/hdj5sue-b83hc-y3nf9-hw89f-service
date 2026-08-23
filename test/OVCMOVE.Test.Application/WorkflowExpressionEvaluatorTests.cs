using System.Text.Json;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Workflows.Command;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class WorkflowExpressionEvaluatorTests
{
    [Fact]
    public void BooleanEquals_ReturnsTrueForTwoTrueValues()
    {
        Assert.True(Evaluate("equals", true, true));
    }

    [Fact]
    public void BooleanNotEquals_ReturnsTrueForDifferentValues()
    {
        Assert.True(Evaluate("not_equals", true, false));
    }

    [Theory]
    [InlineData("true")]
    [InlineData(1)]
    public void BooleanComparedWithAnotherType_IsRejected(object right)
    {
        Assert.Throws<ApplicationValidationException>(() =>
            Evaluate("equals", true, right));
    }

    private static bool Evaluate(string @operator, object left, object right)
    {
        var config = JsonSerializer.SerializeToElement(new
        {
            left = new { kind = "literal", value = left },
            @operator,
            right = new { kind = "literal", value = right }
        });

        return WorkflowExpressionEvaluator.EvaluateCondition(
            config,
            new Workflow(),
            new WorkflowExecutionInputModel(),
            new Dictionary<string, JsonElement>());
    }
}
