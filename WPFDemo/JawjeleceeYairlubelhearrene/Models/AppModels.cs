using VolcEngineSdk.OpenSpeech;

namespace JawjeleceeYairlubelhearrene;

internal sealed record SpeakerOption(string DisplayName, string VoiceType, string Language, string Scenario);

internal sealed record PowerPointSlideInfo(int SlideIndex, string SlideText, System.IO.FileInfo SlideImageFile);

internal sealed record PowerPointReadResult(System.IO.FileInfo SourceFile, IReadOnlyList<PowerPointSlideInfo> Slides);

internal sealed record CoursewareSlideMaterialInfo(System.IO.FileInfo SlideThumbnailFilePath, string ContentText);

internal sealed record CoursewareMaterialInfo(IReadOnlyList<CoursewareSlideMaterialInfo> SlideMaterialInfoList);

internal sealed record CoursewareSpeechSlideInfo(string PlainScriptText, System.IO.FileInfo SlideThumbnailFile);

internal sealed record CoursewareSpeechInfo(IReadOnlyList<CoursewareSpeechSlideInfo> SlideInfoList);

internal sealed record CoursewareSpeechSynthesisOptions(
    OpenSpeechAuthentication Authentication,
    string Speaker,
    string AudioFormat = "mp3",
    int SampleRate = 24000,
    string UsageTokensToReturn = "text_words");

internal sealed record LocalDefaultValues(
    string OpenSpeechApiKey,
    string ResourceId,
    string Speaker,
    string OpenAiApiKey,
    string OpenAiEndpoint,
    string OpenAiModel,
    string FfmpegExecutablePath,
    string OutputDirectoryPath);

internal sealed record SpeechVideoGenerationOptions(
    System.IO.FileInfo FfmpegExecutableFile,
    string OpenSpeechApiKey,
    string ResourceId,
    string Speaker,
    string OpenAiApiKey,
    Uri OpenAiEndpoint,
    string OpenAiModel,
    System.IO.DirectoryInfo OutputDirectory);

internal sealed record SpeechVideoGenerationResult(
    System.IO.FileInfo OutputVideoFile,
    System.IO.DirectoryInfo OutputDirectory,
    CoursewareSpeechInfo CoursewareSpeechInfo);
