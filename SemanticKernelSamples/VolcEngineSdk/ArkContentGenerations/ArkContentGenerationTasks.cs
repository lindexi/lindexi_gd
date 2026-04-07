using System.Text;
using System.Text.Json.Nodes;

namespace VolcEngineSdk;

public class ArkContentGenerationTasks(ArkClient arkClient)
{
    public ArkClient Client => arkClient;

    public async Task<ArkCreateTaskResult> Create(string modelName, IReadOnlyList<ArkContent> contentList)
    {
        var jsonObject = new JsonObject();
        jsonObject.Add("model", modelName);
        var contentArray = new JsonArray();
        foreach (var content in contentList)
        {
            var contentJson = JsonNode.Parse(content.ToJson());
            contentArray.Add(contentJson);
        }
        jsonObject.Add("content", contentArray);

        var httpClient = Client.HttpClient;
        var url = $"{Client.BaseUrl}/contents/generations/tasks";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        var requestJsonText = jsonObject.ToString();
        request.Content = new StringContent(requestJsonText, Encoding.UTF8, "application/json");
        Client.AppendAuthorization(request);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var jsonNode = JsonNode.Parse(jsonResponse);
        var id = jsonNode?["id"]?.ToString();
        return new ArkCreateTaskResult(id);
    }

    public async Task<ArkGetTaskResult> Get(string taskId)
    {
        //  https://ark.cn-beijing.volces.com/api/v3/contents/generations/tasks/{id} 
        var httpClient = Client.HttpClient;
        var url = $"{Client.BaseUrl}/contents/generations/tasks/{taskId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        Client.AppendAuthorization(request);
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();

        // {"id":"cgt-20260119201255-2tnnx","model":"doubao-seedance-1-5-pro-251215","status":"running","created_at":1768824776,"updated_at":1768824776,"service_tier":"default","execution_expires_after":172800,"generate_audio":true,"draft":false}
        var jsonNode = JsonNode.Parse(jsonResponse)!;
        var id = jsonNode["id"]!.ToString();
        var model = jsonNode["model"]!.ToString();
        var statusString = jsonNode["status"]!.ToString();
        var status = Enum.Parse<ArkTaskStatus>(statusString, ignoreCase: true);

        ArkGeneratedVideoContent? content = null;
        var contentJsonNode = jsonNode["content"];
        var videoUrl = contentJsonNode?["video_url"]?.ToString();
        if (!string.IsNullOrEmpty(videoUrl))
        {
            content = new ArkGeneratedVideoContent(videoUrl)
            {
                LastFrameUrl = contentJsonNode!["last_frame_url"]?.ToString()
            };
        }

        return new ArkGetTaskResult()
        {
            TaskId = id,
            Model = model,
            Status = status,
            Content = content
        };
    }
}