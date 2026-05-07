namespace JawjeleceeYairlubelhearrene.Models;

internal sealed record SpeechVideoGenerationOptions(
    System.IO.FileInfo FfmpegExecutableFile,
    string OpenSpeechApiKey,
    string ResourceId,
    string Speaker,
    string OpenAiApiKey,
    Uri OpenAiEndpoint,
    string OpenAiModel,
    System.IO.DirectoryInfo OutputDirectory);