using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Test.Application;

public sealed class FunctionCardInputDefinitionTests
{
    [Fact]
    public void GetKeys_ReturnsConfiguredInputKeys()
    {
        var keys = FunctionCardInputDefinition.GetKeys(
            """
            [
              { "key": "target_team", "type": "team" },
              { "key": "amount", "type": "number" }
            ]
            """);

        Assert.Equal(2, keys.Count);
        Assert.Contains("target_team", keys);
        Assert.Contains("amount", keys);
    }

    [Fact]
    public void GetKeys_RejectsDuplicateInputKeys()
    {
        Assert.Throws<ApplicationValidationException>(() =>
            FunctionCardInputDefinition.GetKeys(
                """
                [
                  { "key": "target_team" },
                  { "key": "target_team" }
                ]
                """));
    }
}
