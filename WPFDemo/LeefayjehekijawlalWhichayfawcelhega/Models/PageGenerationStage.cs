namespace LeefayjehekijawlalWhichayfawcelhega.Models;

internal enum PageGenerationStage
{
    PendingPromptGeneration,
    PromptReady,
    AwaitingImageGeneration,
    GeneratingImages,
    ImagesReady,
    Failed,
    Exported,
}
