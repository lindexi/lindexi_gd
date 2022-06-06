using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace GeacejalcurnawLarjearemwhear;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Debug.WriteLine(ProcessHelper.GetCurrentProcessSessionId());
        var process = Process.GetCurrentProcess();
    }
}

internal static class ProcessHelper
{
    public static int GetCurrentProcessId()
    {
#if NET6_0_OR_GREATER
        return Environment.ProcessId;
#else
        return Process.GetCurrentProcess().Id;
#endif
    }

    public static uint GetCurrentProcessSessionId()
    {
        if (_sessionId != null)
        {
            return _sessionId.Value;
        }

        var result = ProcessIdToSessionId((uint) GetCurrentProcessId(), out var sessionId);
        _sessionId = sessionId;
        return sessionId;
    }

    private static uint? _sessionId;

    [DllImport("kernel32.dll")]
    static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);
}