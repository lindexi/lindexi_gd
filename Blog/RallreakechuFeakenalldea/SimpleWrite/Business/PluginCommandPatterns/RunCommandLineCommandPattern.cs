using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

sealed class RunCommandLineCommandPattern : ICommandPattern
{
    private static readonly Lock RunningProcessLock = new();
    private static readonly List<Process> RunningProcessList = [];

    public bool SupportSingleLine => true;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        return ValueTask.FromResult(true);
    }

    public string Title => "在终端运行";

    public async Task DoAsync(string text, TextEditor textEditor)
    {
        _ = textEditor;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = false,
                WorkingDirectory = Environment.CurrentDirectory,
            },
            EnableRaisingEvents = true,
        };

        process.Exited += (_, _) => UnregisterProcess(process);
        if (!process.Start())
        {
            return;
        }

        RegisterProcess(process);

        process.StandardInput.AutoFlush = true;
        await process.StandardInput.WriteLineAsync(text);
        await process.StandardInput.FlushAsync();
    }

    private static void RegisterProcess(Process process)
    {
        lock (RunningProcessLock)
        {
            RunningProcessList.Add(process);
        }
    }

    private static void UnregisterProcess(Process process)
    {
        lock (RunningProcessLock)
        {
            RunningProcessList.Remove(process);
        }

        process.Dispose();
    }
}