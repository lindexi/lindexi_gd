using System.IO;
using JawjeleceeYairlubelhearrene.Models;

namespace JawjeleceeYairlubelhearrene.Services;

internal static class LocalDefaultsProvider
{
    private const string LindexiRootDirectory = @"C:\lindexi";
    private const string OpenSpeechApiKeyFileName = "OpenSpeech API Key.txt";
    private const string OpenAiApiKeyFileName = "Doubao.txt";
    private const string FfmpegExecutableFileName = "ffmpeg.exe";
    private static readonly string LindexiOpenSpeechApiKeyPath = Path.Combine(LindexiRootDirectory, "Work", "Key", OpenSpeechApiKeyFileName);
    private static readonly string LindexiOpenAiApiKeyPath = Path.Combine(LindexiRootDirectory, "Work", OpenAiApiKeyFileName);
    private static readonly string LindexiFfmpegExecutablePath = Path.Combine(LindexiRootDirectory, "Application", FfmpegExecutableFileName);

    public static LocalDefaultValues Load()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var outputDirectory = Path.Combine(baseDirectory, "GeneratedVideos");

        return new LocalDefaultValues(
            OpenSpeechApiKey: ReadOptionalText(ResolvePreferredFilePath(baseDirectory, OpenSpeechApiKeyFileName, LindexiOpenSpeechApiKeyPath)),
            ResourceId: "seed-tts-2.0",
            Speaker: "zh_female_vv_uranus_bigtts",
            OpenAiApiKey: ReadOptionalText(ResolvePreferredFilePath(baseDirectory, OpenAiApiKeyFileName, LindexiOpenAiApiKeyPath)),
            OpenAiEndpoint: "https://ark.cn-beijing.volces.com/api/v3",
            OpenAiModel: "ep-20260306101224-c8mtg",
            FfmpegExecutablePath: ResolvePreferredFilePath(baseDirectory, FfmpegExecutableFileName, LindexiFfmpegExecutablePath),
            OutputDirectoryPath: outputDirectory);
    }

    private static string ResolvePreferredFilePath(string baseDirectory, string fileName, string legacyFilePath)
    {
        var appLocalFilePath = Path.Combine(baseDirectory, fileName);
        if (File.Exists(appLocalFilePath))
        {
            return appLocalFilePath;
        }

        if (Directory.Exists(LindexiRootDirectory) && File.Exists(legacyFilePath))
        {
            return legacyFilePath;
        }

        return string.Empty;
    }

    private static string ReadOptionalText(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return string.Empty;
        }

        return File.ReadAllText(filePath).Trim();
    }
}
