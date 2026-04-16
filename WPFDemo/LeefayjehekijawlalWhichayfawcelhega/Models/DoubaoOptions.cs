namespace LeefayjehekijawlalWhichayfawcelhega.Models;

internal sealed class DoubaoOptions
{
    public required string Endpoint { get; init; }

    public required string PromptModelId { get; init; }

    public required string ImageModelId { get; init; }

    public required string ApiKeyEnvironmentVariableName { get; init; }

    public int CandidateImageCount { get; init; } = 4;

    public string ImageSize { get; init; } = "1024x1024";
}
