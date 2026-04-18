namespace LeefayjehekijawlalWhichayfawcelhega.Models;

internal sealed class AiServiceOptions
{
    public required string Endpoint { get; set; }

    public required string PromptModelId { get; set; }

    public required string ImageModelId { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public int CandidateImageCount { get; set; } = 4;
}
