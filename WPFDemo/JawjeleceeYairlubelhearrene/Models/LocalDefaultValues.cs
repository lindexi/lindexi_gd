namespace JawjeleceeYairlubelhearrene.Models;

internal sealed record LocalDefaultValues(
    string OpenSpeechApiKey,
    string ResourceId,
    string Speaker,
    string OpenAiApiKey,
    string OpenAiEndpoint,
    string OpenAiModel,
    string FfmpegExecutablePath,
    string OutputDirectoryPath);