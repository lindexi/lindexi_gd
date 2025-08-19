using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RallcewawhahereLunucaje;

static partial class IndependentProcessLogFolderManager
{
    public static string GetLogFolderName(DateTime now)
    {
        // 命名规则： 年月日_小时分钟秒,进程ID
        // 20250724_171139,63912
        int processId = Environment.ProcessId;
        return $"{now:yyyyMMdd_HHmmss},{processId}";
    }

    public static string GetLogFolderName(DateTime now, int processId)
        => $"{now:yyyyMMdd_HHmmss},{processId}";

    /// <summary>
    /// 注册定时清理日志任务
    /// </summary>
    public static void RegisterRegularCleaningLogTasks(string rootLogDirectory)
    {
        // 延迟 1 分钟后执行日志清理任务
        Task.Delay(TimeSpan.FromMinutes(1))
            .ContinueWith(async _ =>
            {
                var regularCleaningLogTask = new RegularCleaningLogTask(rootLogDirectory);
                await regularCleaningLogTask.Run();
            });
    }

    public static MatchIndependentProcessLogFolderResult TryMatchLogFolderName(string folderName)
    {
        var regex = GetLogFolderProcessRegex();
        var match = regex.Match(folderName);
        if (match.Success && match.Groups.Count == 2)
        {
            if (int.TryParse(match.Groups[1].Value, out var processId))
            {
                return new MatchIndependentProcessLogFolderResult(true, processId);
            }
        }

        return new MatchIndependentProcessLogFolderResult(false, -1);
    }

    [GeneratedRegex(@"\d+_\d+,(\d+)")]
    private static partial Regex GetLogFolderProcessRegex();
}

readonly record struct MatchIndependentProcessLogFolderResult(bool IsMatch, int ProcessId);
