using OVCMOVE.Api.Common;

namespace OVCMOVE.Test.Application;

public sealed class JsonPayloadDeserializerTests
{
    [Fact]
    public void Deserialize_UsesWebJsonDefaults()
    {
        var result = JsonPayloadDeserializer.Deserialize<PayloadModel>(
            """{"isHidden":true}""");

        Assert.True(result.IsHidden);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("{")]
    public void Deserialize_RejectsInvalidPayload(string payload)
    {
        Assert.Throws<ArgumentException>(() =>
            JsonPayloadDeserializer.Deserialize<PayloadModel>(payload));
    }

    private sealed class PayloadModel
    {
        public bool IsHidden { get; init; }
    }
}
