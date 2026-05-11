namespace JawjeleceeYairlubelhearrene.Models;

internal sealed record SpeechVideoGenerationOptions(
    System.IO.FileInfo FfmpegExecutableFile,
    bool EnableWatermark,
    string WatermarkText,
    string OpenSpeechApiKey,
    string ResourceId,
    string Speaker,
    string OpenAiApiKey,
    Uri OpenAiEndpoint,
    string OpenAiModel,
    System.IO.DirectoryInfo OutputDirectory);