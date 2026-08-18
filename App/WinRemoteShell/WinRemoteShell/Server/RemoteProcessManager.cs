using System.Diagnostics;
using WinRemoteShell.Shared;

namespace WinRemoteShell.Server;

internal sealed class RemoteProcessManager
{
    public ProcessListResponse List(bool includeDetails)
    {
        var processes = Process.GetProcesses()
            .Select(process => CreateProcessInfo(process, includeDetails))
            .Where(process => process is not null)
            .Select(process => process!)
            .OrderBy(process => process.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(process => process.Id)
            .ToArray();

        return new ProcessListResponse(processes);
    }

    public KillProcessesResponse Kill(KillProcessesRequest request)
    {
        var targets = FindTargets(request).ToArray();
        var results = targets.Select(process => KillProcess(process, request.KillTree)).ToArray();
        return new KillProcessesResponse(results);
    }

    private static IEnumerable<Process> FindTargets(KillProcessesRequest request)
    {
        if (request.ProcessId is { } processId)
        {
            Process? process;
            try
            {
                process = Process.GetProcessById(processId);
            }
            catch (ArgumentException)
            {
                process = null;
            }

            if (process is not null)
            {
                yield return process;
            }

            yield break;
        }

        var requestedName = Path.GetFileNameWithoutExtension(request.ProcessName);
        foreach (var process in Process.GetProcesses())
        {
            string processName;
            try
            {
                processName = process.ProcessName;
            }
            catch (InvalidOperationException)
            {
                process.Dispose();
                continue;
            }

            if (string.Equals(processName, requestedName, StringComparison.OrdinalIgnoreCase))
            {
                yield return process;
            }
            else
            {
                process.Dispose();
            }
        }
    }

    private static KillProcessResult KillProcess(Process process, bool killTree)
    {
        using (process)
        {
            var processId = process.Id;
            var processName = TryGet(() => process.ProcessName) ?? string.Empty;
            try
            {
                process.Kill(killTree);
                return new KillProcessResult(processId, processName, true, null);
            }
            catch (InvalidOperationException)
            {
                return new KillProcessResult(processId, processName, true, null);
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                return new KillProcessResult(processId, processName, false, exception.Message);
            }
            catch (NotSupportedException exception)
            {
                return new KillProcessResult(processId, processName, false, exception.Message);
            }
        }
    }

    private static RemoteProcessInfo? CreateProcessInfo(Process process, bool includeDetails)
    {
        using (process)
        {
            try
            {
                var name = process.ProcessName;
                if (!includeDetails)
                {
                    return new RemoteProcessInfo(process.Id, name, null, null, null, null, null);
                }

                return new RemoteProcessInfo(
                    process.Id,
                    name,
                    TryGet(() => process.MainModule?.FileName),
                    TryGet(() => process.StartTime.ToUniversalTime()),
                    TryGet(() => process.WorkingSet64),
                    TryGet(() => process.PrivateMemorySize64),
                    TryGet(() => process.Threads.Count));
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    private static T? TryGet<T>(Func<T> valueFactory)
    {
        try
        {
            return valueFactory();
        }
        catch (InvalidOperationException)
        {
            return default;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return default;
        }
        catch (NotSupportedException)
        {
            return default;
        }
    }
}
