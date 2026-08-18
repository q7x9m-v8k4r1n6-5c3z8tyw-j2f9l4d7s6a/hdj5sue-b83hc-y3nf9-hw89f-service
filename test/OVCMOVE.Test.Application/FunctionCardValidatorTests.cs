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
    public void UnknownCategory_IsRejected()
    {
        Assert.Throws<ApplicationValidationException>(() =>
            FunctionCardValidator.Validate(
                "card-key", "Card name", "", "unknown", null,
                JsonSerializer.SerializeToElement(Array.Empty<object>())));
    }
}
