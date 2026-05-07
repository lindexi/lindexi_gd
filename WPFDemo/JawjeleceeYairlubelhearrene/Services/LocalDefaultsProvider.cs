using System.IO;
using JawjeleceeYairlubelhearrene.Models;

namespace JawjeleceeYairlubelhearrene.Services;

internal static class LocalDefaultsProvider
{
    private const string DefaultOpenSpeechApiKeyPath = @"C:\lindexi\Work\Key\OpenSpeech API Key.txt";
    private const string DefaultOpenAiApiKeyPath = @"C:\lindexi\Work\Doubao.txt";
    private const string DefaultFfmpegExecutablePath = @"C:\lindexi\Application\ffmpeg.exe";

    public static LocalDefaultValues Load()
    {
        var outputDirectory = Path.Combine(AppContext.BaseDirectory, "GeneratedVideos");

        return new LocalDefaultValues(
            OpenSpeechApiKey: ReadOptionalText(DefaultOpenSpeechApiKeyPath),
            ResourceId: "seed-tts-2.0",
            Speaker: "zh_female_vv_uranus_bigtts",
            OpenAiApiKey: ReadOptionalText(DefaultOpenAiApiKeyPath),
            OpenAiEndpoint: "https://ark.cn-beijing.volces.com/api/v3",
            OpenAiModel: "ep-20260306101224-c8mtg",
            FfmpegExecutablePath: File.Exists(DefaultFfmpegExecutablePath) ? DefaultFfmpegExecutablePath : string.Empty,
            OutputDirectoryPath: outputDirectory);
    }

    private static string ReadOptionalText(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        return File.ReadAllText(filePath).Trim();
    }
}
