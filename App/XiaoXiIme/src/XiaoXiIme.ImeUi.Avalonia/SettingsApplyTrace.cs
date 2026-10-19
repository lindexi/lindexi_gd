using System.Diagnostics;
using System.Text.Json;
using Avalonia.Threading;

namespace XiaoXiIme.ImeUi.Avalonia;

internal static class SettingsApplyTrace
{
    internal static void Mark(string stage, long started)
    {
        var path = Environment.GetEnvironmentVariable("XIAOXIIME_VALIDATION_TRACE");
        if (string.IsNullOrWhiteSpace(path)) return;
        File.AppendAllText(path + ".apply.jsonl", JsonSerializer.Serialize(new
        {
            time = DateTimeOffset.UtcNow, stage, elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            thread = Environment.CurrentManagedThreadId, ui = Dispatcher.UIThread.CheckAccess(), pid = Environment.ProcessId
        }) + Environment.NewLine);
    }
}
