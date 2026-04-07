namespace VolcEngineSdk;

public class ArkImageContent(string imageUrl) : ArkContent
{
    public string ImageUrl => imageUrl;
    public override string ToJson()
    {
        return
            $$"""
              {
                 "type": "image_url",
                 "image_url": 
                 {
                     "url": "{{ImageUrl}}" 
                 }
              }
              """;
    }
}