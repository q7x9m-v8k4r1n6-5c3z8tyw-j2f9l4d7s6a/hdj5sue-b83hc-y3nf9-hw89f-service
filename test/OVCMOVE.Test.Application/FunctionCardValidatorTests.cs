using System.Text.Json;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Test.Application;

public sealed class FunctionCardValidatorTests
{
    [Theory]
    [InlineData("attack")]
    [InlineData("defense")]
    [InlineData("effect")]
    public void ValidCardCategories_AreAccepted(string category)
    {
        FunctionCardValidator.Validate(
            "card-key", "Card name", "", category, null,
            JsonSerializer.SerializeToElement(Array.Empty<object>()));
    }

    [Fact]
    public void Inputs_MustBeJsonArray()
    {
        Assert.Throws<ApplicationValidationException>(() =>
            FunctionCardValidator.Validate(
                "card-key", "Card name", "", "attack", null,
                JsonSerializer.SerializeToElement(new { field = "value" })));
    }

    [Fact]
    public void InputWithKey_IsAccepted()
    {
        var inputs = JsonSerializer.SerializeToElement(new[]
        {
            new { key = "target_team", label = "Đội mục tiêu" }
        });

        FunctionCardValidator.Validate(
            "card-key", "Card name", "", "attack", null, inputs);
    }

    [Fact]
    public void UnknownCategory_IsRejected()
    {
        Assert.Throws<ApplicationValidationException>(() =>
            FunctionCardValidator.Validate(
                "card-key", "Card name", "", "unknown", null,
                JsonSerializer.SerializeToElement(Array.Empty<object>())));
    }

    [Fact]
    public void InputWithoutKey_IsRejected()
    {
        var inputs = JsonSerializer.SerializeToElement(new[]
        {
            new { label = "Đội mục tiêu" }
        });

        Assert.Throws<ApplicationValidationException>(() =>
            FunctionCardValidator.Validate(
                "card-key", "Card name", "", "attack", null, inputs));
    }

    [Fact]
    public void DuplicateInputKeys_AreRejected()
    {
        var inputs = JsonSerializer.SerializeToElement(new[]
        {
            new { key = "target_team" },
            new { key = "target_team" }
        });

        Assert.Throws<ApplicationValidationException>(() =>
            FunctionCardValidator.Validate(
                "card-key", "Card name", "", "attack", null, inputs));
    }
}
