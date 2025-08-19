using DotNetCampus.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RallcewawhahereLunucaje;

/// <summary>
/// 定时的日志清理任务
/// </summary>
class RegularCleaningLogTask
{
    /// <summary>
    /// 定时的日志清理任务
    /// </summary>
    /// <param name="rootLogDirectory"></param>
    public RegularCleaningLogTask(string rootLogDirectory)
    {
        RootLogDirectory = rootLogDirectory;
    }

    private string RootLogDirectory { get; }

    private Mutex? _mutex;

    public async Task Run()
    {
        // 使用一个 Mutex 确保只有一个实例在运行清理任务。传入的 Mutex 名称需要是唯一的，于是决定使用 Base64 编码的日志目录路径。为什么不能直接使用日志目录路径呢？因为路径包含斜杠等字符，导致 Mutex 名称不合法
        var base64StringForMutexName = Convert.ToBase64String(MemoryMarshal.AsBytes(RootLogDirectory.AsSpan()));

        _mutex = new Mutex(true, base64StringForMutexName, out var createdNew);

        if (!createdNew)
        {
            // 非当前创建的 Mutex 实例，说明有其他实例正在运行清理任务
            _mutex.Dispose();
            return;
        }

        while (true)
        {
            await CleanLogAsync();

            // 日志清理以天为单位，执行一次之后等待一天
            await Task.Delay(TimeSpan.FromDays(1));
        }
    }

    private async Task CleanLogAsync()
    {
        // 日志清理逻辑，删除超过 7 天的日志文件，且要求进程不存在
        // 可能会存在重名的进程，此时也不做处理
        // 直接清理就好，不移动到 NoToday 里面，实际用了这么久，发现移动到 NoToday 里面只会增加复杂度，并没有什么实际好处
        var maxTime = TimeSpan.FromDays(7); // 暂不开放设置，固定 7 天即可
        try
        {
            Process[]? processes = null;

            var now = DateTime.Now;
            var rootLogDirectory = new DirectoryInfo(RootLogDirectory);
            var directoryInfos = rootLogDirectory.GetDirectories();
            // 如果一个文件夹是不认识的，那也扬了
            foreach (var directoryInfo in directoryInfos)
            {
                try
                {
                    if (now - directoryInfo.CreationTime < maxTime)
                    {
                        continue;
                    }

                    var (isMatch, processId) = IndependentProcessLogFolderManager.TryMatchLogFolderName(directoryInfo.Name);

                    if (!isMatch)
                    {
                        // 不认识的，直接删除
                        directoryInfo.Delete(recursive: true);
                    }
                    else
                    {
                        processes ??= Process.GetProcesses();
                        var process = processes.FirstOrDefault(p => p.Id == processId);
                        if (process is null)
                        {
                            // 进程不存在，直接删除
                            directoryInfo.Delete(recursive: true);
                        }
                        else
                        {
                            // 进程存在，不删除
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"[RegularCleaningLogTask] CleanLogAsync Fail to clean {directoryInfo}.", e);
                }

                // 删除的空隙需要插入等待，避免删除过快导致大量 IO 出现磁盘忙
                await Task.Delay(TimeSpan.FromMilliseconds(15));
            }
        }
        catch (Exception e)
        {
            Log.Warn("[RegularCleaningLogTask] CleanLogAsync Fail.", e);
        }
    }
}