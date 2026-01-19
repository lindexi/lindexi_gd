using System.Text.Json;

namespace VolcEngineSdk;

public class Class1
{

}


public class ArkClient
{
    public ArkClient(string apiKey, string baseUrl= "https://ark.cn-beijing.volces.com/api/v3")
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl;
    }

    private readonly string _apiKey;

    private readonly string _baseUrl;
}

public class ArkContentGeneration
{

}

public class ArkContentGenerationTask
{
    public async Task Create(string modelName, IReadOnlyList<ArkContent> contentList)
    {

    }
}

public abstract class ArkContent
{
    public abstract string ToJson();
}

public class ArkTextContent(string text): ArkContent
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