using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MediaFoundation.Core;
using MediaFoundation.Controls;

namespace MediaFoundation.PlayerDemo;

internal static class LocalizationTest
{
    internal static void Run(Action<bool, string> check)
    {
        string originalLanguage = Lang.Current.IetfLanguageTag;
        string originalCoreLanguage = SR.Current.IetfLanguageTag;
        string originalWpfLanguage = Text.Current.IetfLanguageTag;
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Lang.SetCurrent("zh-Hans");
            check((string)Lang.Current.DemoTitle == "Media Foundation 播放器", "LocalizationChineseTitle");
            check((string)Lang.Current.SubtitleMenu == "字幕", "LocalizationChineseSubtitleMenu");
            check(Lang.Current.SubtitleTrack.ToString("2", "English") == "轨道 2 · English", "LocalizationChineseTrackFormat");
            check(Lang.Current.SubtitleUnsupportedTrack.ToString("Track", "codec") == "Track（不可用：codec）", "LocalizationChineseUnsupportedFormat");
            check(Lang.Current.SubtitleDelay.ToString("+0.5") == "重置延迟（当前 +0.5 秒）", "LocalizationChineseDelayFormat");
            check(Lang.Current.SubtitleFailure.ToString("error") == "字幕操作失败：error", "LocalizationChineseSubtitleFailureFormat");
            check(Lang.Current.PrototypeStatusText.ToString("Playing", TimeSpan.FromSeconds(2), 42L) == "MFPlay + 自定义 EVR | Playing | 00:00:02 | 实际帧数=42", "LocalizationChinesePrototypeFormat");
            check(Lang.Current.DemoPlaybackFailedText.ToString("error", "log") == "播放失败：error。诊断日志：log", "LocalizationChinesePlaybackFormat");
            check(Lang.Current.DemoLogFailureText.ToString("log", "error") == "诊断日志不可用：log。错误：error。播放可继续，请保留此信息。", "LocalizationChineseLogFormat");
            check(Lang.Current.DemoDiagnosticActionFailedText.ToString("error", "log") == "诊断操作失败：error。日志路径：log", "LocalizationChineseDiagnosticFormat");
            check(Lang.Current.DemoSwitchFailedText.ToString("error", "log") == "切换失败：error。诊断日志：log。可在“更多”中重新选择后端重试。", "LocalizationChineseSwitchFormat");
            check(Lang.Current.DemoMediaProbeResultText.ToString("report", "log") == "媒体诊断（不代表播放成功）：report\n日志：log\nregisteredCandidates=0 表示未枚举到匹配编码的 MFT；大于 0 仍需验证编码 profile/位深。EBML type=1 为视频、2 为音频、17 为字幕；codec 为容器声明的编码。若 MF 解析失败，仅有 EBML 信息不能证明解码器缺失。请保留报告以确定具体轨道；不要通过跳过音轨认定完整播放成功。", "LocalizationChineseProbeFormat");

            Lang.SetCurrent("en");
            check((string)Lang.Current.DemoTitle == "Media Foundation Player", "LocalizationEnglishTitle");
            check((string)Lang.Current.SubtitleMenu == "Subtitles", "LocalizationEnglishSubtitleMenu");
            check(Lang.Current.SubtitleTrack.ToString("2", "English") == "Track 2 · English", "LocalizationEnglishTrackFormat");
            check(Lang.Current.SubtitleUnsupportedTrack.ToString("Track", "codec") == "Track (unavailable: codec)", "LocalizationEnglishUnsupportedFormat");
            check(Lang.Current.SubtitleDelay.ToString("-0.5") == "Reset delay (currently -0.5 seconds)", "LocalizationEnglishDelayFormat");
            check(Lang.Current.SubtitleFailure.ToString("error") == "Subtitle operation failed: error", "LocalizationEnglishSubtitleFailureFormat");
            check(Lang.Current.PrototypeStatusText.ToString("Playing", TimeSpan.FromSeconds(2), 42L) == "MFPlay + custom EVR | Playing | 00:00:02 | actualFrames=42", "LocalizationEnglishPrototypeFormat");
            check(Lang.Current.DemoPlaybackFailedText.ToString("error", "log") == "Playback failed: error. Diagnostic log: log", "LocalizationEnglishPlaybackFormat");
            check(Lang.Current.DemoLogFailureText.ToString("log", "error") == "Diagnostic log unavailable: log. Error: error. Playback can continue; keep this information.", "LocalizationEnglishLogFormat");
            check(Lang.Current.DemoDiagnosticActionFailedText.ToString("error", "log") == "Diagnostic action failed: error. Log path: log", "LocalizationEnglishDiagnosticFormat");
            check(Lang.Current.DemoSwitchFailedText.ToString("error", "log") == "Switch failed: error. Diagnostic log: log. Select the backend again under More to retry.", "LocalizationEnglishSwitchFormat");
            check(Lang.Current.DemoMediaProbeResultText.ToString("report", "log") == "Media diagnostics (does not imply successful playback): report\nLog: log\nregisteredCandidates=0 means no MFT matching the codec was enumerated; a positive count still requires codec profile/bit-depth verification. EBML type=1 is video, 2 is audio, and 17 is subtitles; codec is declared by the container. If MF parsing fails, EBML information alone does not prove a missing decoder. Keep the report to identify the affected track; skipping audio does not establish successful playback of the complete media.", "LocalizationEnglishProbeFormat");

            Lang.SetCurrent("en-US");
            check((string)Lang.Current.DemoTitle == "Media Foundation Player", "LocalizationEnglishRegionFallback");
            Lang.SetCurrent("zh-CN");
            check((string)Lang.Current.DemoTitle == "Media Foundation 播放器", "LocalizationChineseRegionFallback");
            Lang.SetCurrent("fr-FR");
            check((string)Lang.Current.DemoTitle == "Media Foundation 播放器", "LocalizationDefaultFallback");
            check((string)Lang.Default.DemoTitle == "Media Foundation 播放器", "LocalizationDefaultLanguage");
            VerifyCoreAndWpf(check);
            foreach (var assembly in new[] { typeof(Lang).Assembly, typeof(SR).Assembly, typeof(Text).Assembly })
            {
                check(!assembly.GetReferencedAssemblies().Any(reference => IsLocalizationDependency(reference.Name)),
                    $"LocalizationNoRuntimeAssemblyReference_{assembly.GetName().Name}");
                check(!assembly.GetManifestResourceNames().Any(name => name.EndsWith(".resources", StringComparison.Ordinal)
                    && !name.EndsWith(".g.resources", StringComparison.Ordinal)),
                    $"LocalizationNoLegacyResourceManager_{assembly.GetName().Name}");
            }
            check(!Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll", SearchOption.AllDirectories)
                .Any(path => IsLocalizationDependency(Path.GetFileNameWithoutExtension(path))), "LocalizationNoPublishedRuntimeDll");
            using var deps = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MediaFoundation.PlayerDemo.deps.json")));
            check(!deps.RootElement.GetProperty("libraries").EnumerateObject()
                .Any(library => IsLocalizationDependency(library.Name.Split('/')[0])), "LocalizationNoPublishedDepsLibrary");
            check(!deps.RootElement.GetProperty("targets").EnumerateObject().SelectMany(target => target.Value.EnumerateObject())
                .Any(library => IsLocalizationDependency(library.Name.Split('/')[0])), "LocalizationNoPublishedDepsTarget");
        }
        finally
        {
            Lang.SetCurrent(originalLanguage);
            SR.SetCurrent(originalCoreLanguage);
            Text.SetCurrent(originalWpfLanguage);
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static bool IsLocalizationDependency(string? name) =>
        name?.StartsWith("DotNetCampus.", StringComparison.OrdinalIgnoreCase) == true
        && name.Contains("Localization", StringComparison.OrdinalIgnoreCase);

    private static void VerifyCoreAndWpf(Action<bool, string> check)
    {
        SR.SetCurrent("zh-Hans");
        check((string)SR.Current.SubtitleEmpty == "未找到字幕片段。", "LocalizationCoreChinese");
        CheckDiagnostic("SubRip 字幕第 1 行：未找到字幕片段。", "LocalizationCoreChineseDiagnostic", check);
        SR.SetCurrent("en");
        check((string)SR.Current.SubtitleEmpty == "No subtitle cues were found.", "LocalizationCoreEnglish");
        CheckDiagnostic("SubRip subtitle line 1: No subtitle cues were found.", "LocalizationCoreEnglishDiagnostic", check);

        Text.SetCurrent("zh-Hans");
        check((string)Text.Current.PlayerOpenText == "打开文件", "LocalizationWpfChinese");
        var block = new TextBlock();
        var expression = BindingOperations.SetBinding(block, TextBlock.TextProperty,
            new Binding("PlayerOpenText") { Source = Text.Current, Mode = BindingMode.OneWay });
        try
        {
            check(expression.Status == BindingStatus.Active && block.Text == "打开文件", "LocalizationBindingChinese");
            Text.SetCurrent("en");
            check((string)Text.Current.PlayerOpenText == "Open file", "LocalizationWpfEnglish");
            check(block.Text == "Open file", "LocalizationBindingEnglishNotification");
            Text.SetCurrent("zh-Hans");
            check(block.Text == "打开文件", "LocalizationBindingChineseNotification");
        }
        finally
        {
            BindingOperations.ClearBinding(block, TextBlock.TextProperty);
        }

        var dictionary = new ResourceDictionary
        {
            Source = new Uri("/MediaFoundation.Controls;component/Themes/Generic.xaml", UriKind.Relative)
        };
        check(dictionary[typeof(MediaPlayerControl)] is Style, "LocalizationWpfTemplateResource");
        check(dictionary["PlayerButtonStyle"] is Style, "LocalizationWpfMergedResource");
    }

    private static void CheckDiagnostic(string expected, string name, Action<bool, string> check)
    {
        bool rejected = false;
        try { SubtitleReader.Parse("", SubtitleFormat.SubRip); }
        catch (FormatException error) { rejected = error.Message == expected; }
        check(rejected, name);
    }
}
