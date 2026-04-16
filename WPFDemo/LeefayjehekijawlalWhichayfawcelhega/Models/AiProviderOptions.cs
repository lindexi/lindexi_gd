namespace LeefayjehekijawlalWhichayfawcelhega.Models;

internal sealed class AiProviderOptions
{
    public string Endpoint { get; set; } = "https://ark.cn-beijing.volces.com/api/v3";

    public string ApiKey { get; set; } = string.Empty;

    public string PromptModelId { get; set; } = "ep-20260306101224-c8mtg";

    public string ImageModelId { get; set; } = "ep-20260120102721-c4pxb";

    public int CandidateImageCount { get; set; } = 4;
}
