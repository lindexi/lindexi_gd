using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MediaFoundation.Controls;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class SubtitleTest
{
    internal static async Task RunAsync(CancellationToken token)
    {
        var results = new List<string>();
        void Check(bool success, string name)
        {
            if (!success) throw new InvalidOperationException($"Subtitle test failed: {name}");
            results.Add($"PASS {name}");
        }
        try
        {
            await MatroskaSubtitleTest.RunAsync(Check, token);
            await Mp4SubtitleTest.RunAsync(Check, token);
            LrcFormatTest.Run(Check);
            MicroDvdFormatTest.Run(Check);
            SubViewerFormatTest.Run(Check);
            Mpl2FormatTest.Run(Check);
            var input = new[]
            {
                new SubtitleCue(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), "Later"),
                new SubtitleCue(TimeSpan.Zero, TimeSpan.FromSeconds(3), "Earlier"),
                new SubtitleCue(TimeSpan.Zero, TimeSpan.FromSeconds(1), "SameStart")
            };
            var constructed = new SubtitleDocument(input);
            Check(constructed.GetText(TimeSpan.Zero) == "Earlier\nSameStart", "DocumentStableSort");
            input[1] = new SubtitleCue(TimeSpan.Zero, TimeSpan.FromSeconds(1), "Changed");
            Check(constructed.GetText(TimeSpan.FromSeconds(1)) == "Earlier", "DocumentOwnsSnapshot");
            Check(new SubtitleDocument(Array.Empty<SubtitleCue>()).GetText(TimeSpan.Zero) == "", "EmptyDocument");
            ExpectInvalidCue(new SubtitleCue(TimeSpan.FromSeconds(-1), TimeSpan.Zero, "Text"), Check, "RejectNegativeStart");
            ExpectInvalidCue(new SubtitleCue(TimeSpan.Zero, TimeSpan.Zero, "Text"), Check, "RejectEmptyInterval");
            ExpectInvalidCue(new SubtitleCue(TimeSpan.Zero, TimeSpan.FromSeconds(1), " "), Check, "RejectBlankText");
            ExpectInvalidCue(null, Check, "RejectNullCue");
            const string srt = "1\n00:00:01,000 --> 00:00:04,000\n<b>第一行</b>\n第二行\n\n2\n00:00:02,000 --> 00:00:03,000\n重叠\n";
            var document = SubtitleReader.Parse(srt, SubtitleFormat.SubRip);
            Check(document.GetText(TimeSpan.Zero) == "", "BeforeStart");
            Check(document.GetText(TimeSpan.FromSeconds(1)) == "第一行\n第二行", "InclusiveStartAndMarkup");
            Check(document.GetText(TimeSpan.FromSeconds(2)) == "第一行\n第二行\n重叠", "OverlappingCues");
            Check(document.GetText(TimeSpan.FromSeconds(3)) == "第一行\n第二行", "ExclusiveEnd");
            Check(document.GetText(TimeSpan.FromSeconds(4)) == "", "ClearedAtEnd");
            Check(document.GetText(TimeSpan.FromSeconds(1)) == "第一行\n第二行", "BackwardSeek");
            var vtt = SubtitleReader.Parse("\uFEFFWEBVTT\r\n\r\nNOTE comment\r\nignored\r\n\r\nid\r\n00:01.000 --> 00:02.000\r\n<v speaker>A &amp; B</v>\r\n", SubtitleFormat.WebVtt);
            Check(vtt.GetText(TimeSpan.FromSeconds(1)) == "A & B", "WebVttHeaderCommentIdentifierAndEntity");
            ExpectFormat("WEBVTT\n\n00:01.000 --> 00:02.000 align:start\ntext", SubtitleFormat.WebVtt, Check, "RejectUnsupportedLayout");
            ExpectFormat("1\n2147483647:00:00,000 --> 2147483647:00:01,000\nx", SubtitleFormat.SubRip, Check, "RejectTimeOverflow");
            ExpectFormat("1\n00:00:02,000 --> 00:00:01,000\nx", SubtitleFormat.SubRip, Check, "RejectReversedInterval");
            ExpectFormat("", SubtitleFormat.SubRip, Check, "RejectEmptyDocument");

            string folder = Path.Combine(Path.GetTempPath(), "MediaFoundationSubtitleTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            results.Add($"Files {folder}");
            string file = Path.Combine(folder, "字幕.srt");
            await File.WriteAllTextAsync(file, srt, token);
            Check((await SubtitleReader.LoadAsync(file, token)).Cues.Count == 2, "LoadUtf8File");
            string microFile = Path.Combine(folder, "micro.sub");
            await File.WriteAllTextAsync(microFile, "{1}{1}25\n{25}{50}Micro", token);
            Check((await SubtitleReader.LoadAsync(microFile, token)).GetText(TimeSpan.FromSeconds(1)) == "Micro", "LoadDetectMicroDvd");
            string viewerFile = Path.Combine(folder, "viewer.sub");
            await File.WriteAllTextAsync(viewerFile, "[INFORMATION]\n[END INFORMATION]\n[SUBTITLE]\n00:00:01.00,00:00:02.00\nViewer", token);
            Check((await SubtitleReader.LoadAsync(viewerFile, token)).GetText(TimeSpan.FromSeconds(1)) == "Viewer", "LoadDetectSubViewer");
            string lrcFile = Path.Combine(folder, "lyrics.lrc");
            await File.WriteAllTextAsync(lrcFile, "[00:01.00]Lyrics", token);
            Check((await SubtitleReader.LoadAsync(lrcFile, token)).GetText(TimeSpan.FromSeconds(1)) == "Lyrics", "LoadLrc");
            string mplFile = Path.Combine(folder, "text.mpl");
            await File.WriteAllTextAsync(mplFile, "[10][20]Mpl", token);
            Check((await SubtitleReader.LoadAsync(mplFile, token)).GetText(TimeSpan.FromSeconds(1)) == "Mpl", "LoadMpl2");
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                try { await SubtitleReader.LoadAsync(file, canceled.Token); Check(false, "CanceledLoad"); }
                catch (OperationCanceledException) { Check(true, "CanceledLoad"); }
            }

            foreach (var mode in new[] { VideoRenderingMode.D3DImage, VideoRenderingMode.Hwnd })
                await RunWindowAsync(mode, Check, token);
            results.Add("RESULT PASS");
        }
        catch (Exception error)
        {
            results.Add(error.ToString());
            results.Add("RESULT FAIL");
            throw;
        }
        finally
        {
            await File.WriteAllLinesAsync(Path.Combine(AppContext.BaseDirectory, "subtitle-test.log"), results, CancellationToken.None);
        }
    }

    private static void ExpectInvalidCue(SubtitleCue? cue, Action<bool, string> check, string name)
    {
        try { _ = new SubtitleDocument(new[] { cue! }); check(false, name); }
        catch (ArgumentException error) { check(error.ParamName == "cues" && error.Message.Contains('0'), name); }
    }

    private static void ExpectFormat(string text, SubtitleFormat format, Action<bool, string> check, string name)
    {
        try { SubtitleReader.Parse(text, format); check(false, name); }
        catch (FormatException error) { check(error.Message.Contains("行", StringComparison.Ordinal), name); }
    }

    private static async Task RunWindowAsync(VideoRenderingMode mode, Action<bool, string> check, CancellationToken token)
    {
        var control = new MediaPlayerControl { AutoPlay = false, RenderingMode = mode };
        var window = new Window { Content = control, Width = 700, Height = 450, Left = 80, Top = 80 };
        Exception? failed = null;
        control.Player.Failed += (_, e) => failed = e.Error;
        control.Player.SubtitleFailed += (_, e) => failed = e.Error;
        try
        {
            window.Show();
            var source = new Uri(Path.Combine(AppContext.BaseDirectory, "self-test.mp4"));
            control.Source = source;
            await UntilAsync(() => failed != null || control.Player.IsReady, token);
            if (failed != null) throw new InvalidOperationException($"{mode} playback initialization failed.", failed);
            var doc = SubtitleReader.Parse("1\n00:00:00,000 --> 01:00:00,000\n字幕测试", SubtitleFormat.SubRip);
            control.Player.Subtitle = doc;
            await UntilAsync(() => FindText(control.Player.VideoView) || window.OwnedWindows.Count == 1, token);
            check(mode == VideoRenderingMode.Hwnd ? window.OwnedWindows.Count == 1 : FindText(control.Player.VideoView), $"{mode} VisibleCaption");
            if (mode == VideoRenderingMode.Hwnd)
            {
                Window overlay = window.OwnedWindows[0];
                nint hwnd = new System.Windows.Interop.WindowInteropHelper(overlay).Handle;
                nint owner = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                check(GetWindow(hwnd, 4) == owner, "Hwnd NativeOwner");
                check((GetWindowLong(hwnd, -20) & 0x08000020) == 0x08000020, "Hwnd NoActivateAndInputTransparent");
                window.Left += 100;
                await Task.Delay(150, token);
                check(overlay.Left > 150, "Hwnd FollowMove");
                window.WindowState = WindowState.Minimized;
                await Task.Delay(150, token);
                check(!overlay.IsVisible, "Hwnd MinimizeHidesCaption");
                window.WindowState = WindowState.Normal;
                await UntilAsync(() => overlay.IsVisible, token);
            }
            control.Player.Play();
            await UntilAsync(() => control.Player.Position > TimeSpan.FromMilliseconds(300) || failed != null, token);
            control.Player.Pause();
            await UntilAsync(() => control.Player.State == PlaybackState.Paused || failed != null, token);
            check(failed == null && control.Player.State == PlaybackState.Paused, $"{mode} Pause");
            control.Player.Seek(TimeSpan.Zero);
            await UntilAsync(() => control.Player.Position < TimeSpan.FromMilliseconds(100), token);
            check(control.Player.Subtitle == doc, $"{mode} SeekRetainsDocument");
            control.Player.SubtitleDelay = TimeSpan.FromSeconds(10);
            await Task.Delay(150, token);
            check(mode == VideoRenderingMode.Hwnd ? !window.OwnedWindows[0].IsVisible : !FindText(control.Player.VideoView), $"{mode} DelayHidesCaption");
            control.Player.SubtitleDelay = TimeSpan.Zero;
            await Task.Delay(150, token);
            control.Player.Close();
            check(control.Player.Subtitle == null && window.OwnedWindows.Count == 0, $"{mode} CloseReleasesCaption");
            control.Player.Open(source);
            await UntilAsync(() => control.Player.IsReady || failed != null, token);
            check(control.Player.Subtitle == null && control.Player.SubtitleDelay == TimeSpan.Zero, $"{mode} ReopenClearsSubtitle");
            control.Player.Subtitle = doc;
            await Task.Delay(150, token);
            control.Dispose();
            check(window.OwnedWindows.Count == 0, $"{mode} DisposeReleasesOverlay");
        }
        finally { control.Dispose(); window.Close(); }
    }

    private static bool FindText(DependencyObject root)
    {
        if (root is TextBlock { Text: "字幕测试", IsVisible: true }) return true;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (FindText(VisualTreeHelper.GetChild(root, i))) return true;
        return false;
    }

    private static async Task UntilAsync(Func<bool> condition, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (!condition()) await Task.Delay(30, timeout.Token);
    }

    [DllImport("user32.dll")] private static extern nint GetWindow(nint hwnd, uint command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint hwnd, int index);
}
