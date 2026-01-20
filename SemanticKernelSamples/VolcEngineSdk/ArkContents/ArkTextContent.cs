using System.Text.Json;

namespace VolcEngineSdk;

public class ArkTextContent(string text) : ArkContent
{
    public string Text => text;


    public override string ToJson()
    {
        return
            $$"""
              {
                   "type": "text",
                   "text": "{{JsonEncodedText.Encode(Text).Value}}"
              }
              """;
    }
}