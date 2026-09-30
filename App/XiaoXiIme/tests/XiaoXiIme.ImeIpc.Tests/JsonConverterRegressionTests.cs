using System.Text.Json;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeIpc.Tests;

public class JsonConverterRegressionTests
{
    [Theory]
    [InlineData("null", true)]
    [InlineData("\"\"", true)]
    [InlineData("\"session-1\"", false)]
    public void SessionId_WhenReadThenPreservesUnspecifiedSemantics(string json, bool unspecified)
    {
        var session = JsonSerializer.Deserialize(json, XiaoXiImeIpcJsonSerializerContext.Default.ImeSessionId);
        Assert.Equal(unspecified, session.IsUnspecified);
    }

    [Fact]
    public void GuidelineLevel_WhenSerializedThenUsesCamelCase()
    {
        var json = JsonSerializer.Serialize(ImeGuidelineLevel.NoCandidate, XiaoXiImeIpcJsonSerializerContext.Default.ImeGuidelineLevel);
        Assert.Equal("\"noCandidate\"", json);
    }

    [Fact]
    public void RegisterAction_WhenSerializedThenUsesCamelCase()
    {
        var json = JsonSerializer.Serialize(ImeRegisterWordAction.Unregister, XiaoXiImeIpcJsonSerializerContext.Default.ImeRegisterWordAction);
        Assert.Equal("\"unregister\"", json);
    }

    [Fact]
    public void GenericConverter_WhenUsedThenPreservesCamelCase()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonCamelCaseStringEnumConverter<ImeKeyKind>());
        Assert.Equal("\"candidateSelection\"", JsonSerializer.Serialize(ImeKeyKind.CandidateSelection, options));
    }
}
