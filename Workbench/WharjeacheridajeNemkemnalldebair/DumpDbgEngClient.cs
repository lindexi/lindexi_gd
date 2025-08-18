using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WharjeacheridajeNemkemnalldebair;

// 只列出用到的最少接口定义，按需可扩展
internal static class DumpDbgEngClient
{
    private static Guid IID_IDebugClient = new("27FE5639-8407-4F47-8364-EE118FB08AC8");
    private static Guid IID_IDebugControl = new("5182E668-105E-416E-AD92-24EF800424BA");
    private static readonly Guid IID_IDebugSymbols3 = new("F02FBECC-50AC-4F36-9AD9-C975E8F32FF8");

    [DllImport("dbgeng.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int DebugCreate(ref Guid InterfaceId, out nint Interface);

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("27FE5639-8407-4F47-8364-EE118FB08AC8")]
    private interface IDebugClient
    {
        int AttachKernel(uint flags, string options);                                      // 0
        int GetKernelConnectionOptions(StringBuilder buffer, uint size, out uint needed);  // 1
        int SetKernelConnectionOptions(string options);                                    // 2
        int StartProcessServer(uint flags, string options, nint reserved);                 // 3
        int ConnectProcessServer(string remoteOptions, out ulong server);                 // 4
        int DisconnectProcessServer(ulong server);                                         // 5
        int GetRunningProcessSystemIds(ulong server, uint[] sysIds, uint count, out uint filled);
        int GetRunningProcessSystemIdByExecutableName(ulong server, string exe, uint flags, out uint sysId);
        int GetRunningProcessDescription(ulong server, uint sysId, uint flags,
            StringBuilder exeName, uint exeNameSize, out uint exeNameNeeded,
            StringBuilder description, uint descSize, out uint descNeeded);
        int AttachProcess(ulong server, uint sysId, uint attachFlags);
        int CreateProcess(ulong server, string commandLine, uint createFlags);
        int CreateProcessAndAttach(ulong server, string commandLine, uint createFlags, uint processId, uint attachFlags);
        int GetProcessOptions();
        int AddProcessOptions(uint flags);
        int RemoveProcessOptions(uint flags);
        int SetProcessOptions(uint flags);
        int OpenDumpFile(string file);                 // 用于旧 ANSI
        int WriteDumpFile(string file, uint qualifier);
        int ConnectSession(uint flags, uint historyLimit);
        int StartServer(string options);
        int OutputServers(uint outputControl, string Machine, uint flags);
        int TerminateProcesses();
        int DetachProcesses();
        int EndSession(uint flags);
        int GetExitCode(out uint code);
        int DispatchCallbacks(uint timeout);
        int ExitDispatch(IDebugClient client);
        int CreateClient(out IDebugClient newClient);
        int GetInputCallbacks(out nint callbacks);
        int SetInputCallbacks(nint callbacks);
        int GetOutputCallbacks(out nint callbacks);
        int SetOutputCallbacks(IDebugOutputCallbacksWide callbacks);  // 我们关心
        int GetOutputMask(out uint mask);
        int SetOutputMask(uint mask);
        int GetOtherOutputMask(IDebugClient client, out uint mask);
        int SetOtherOutputMask(IDebugClient client, uint mask);
        int GetOutputWidth(out uint columns);
        int SetOutputWidth(uint columns);
        int GetOutputLinePrefix(StringBuilder buffer, uint size, out uint needed);
        int SetOutputLinePrefix(string prefix);
        int GetIdentity(StringBuilder buffer, uint size, out uint needed);
        int OutputIdentity(uint outputControl, uint flags, string format);
        int GetEventCallbacks(out nint callbacks);
        int SetEventCallbacks(nint callbacks);
        int FlushCallbacks();
        // 后续方法省略
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("4C7FD663-C394-4E26-8EF1-34AD5ED3764C")]
    private interface IDebugControl
    {
        int GetInterrupt();
        int SetInterrupt(uint flags);
        int GetInterruptTimeout(out uint Seconds);
        int SetInterruptTimeout(uint Seconds);
        int GetLogFile(StringBuilder buffer, uint size, out uint needed, out int append);
        int OpenLogFile(string file, int append);
        int CloseLogFile();
        int GetLogMask(out uint mask);
        int SetLogMask(uint mask);
        int Input(StringBuilder buffer, uint size, out uint needed);
        int ReturnInput(string buffer);
        int Output(uint mask, string format);
        int OutputVaList(uint mask, string format, nint args);
        int ControlledOutput(uint outputControl, uint mask, string format);
        int ControlledOutputVaList(uint outputControl, uint mask, string format, nint args);
        int OutputPrompt(uint outputControl, string format);
        int OutputPromptVaList(uint outputControl, string format, nint args);
        int GetPromptText(StringBuilder buffer, uint size, out uint needed);
        int OutputCurrentState(uint outputControl, uint flags);
        int OutputVersionInformation(uint outputControl);
        int GetNotifyEventHandle(out ulong handle);
        int SetNotifyEventHandle(ulong handle);
        int Assemble(ulong offset, string instr, out ulong endOffset);
        int Disassemble(ulong offset, uint flags, StringBuilder buffer, uint size, out uint needed, out ulong endOffset);
        int GetDisassembleEffectiveOffset(out ulong offset);
        int OutputDisassembly(uint outputControl, uint flags, out ulong endOffset);
        int OutputDisassemblyLines(uint outputControl, uint previous, uint total, out uint written, out uint offset, out ulong start, out ulong end);
        int GetNearInstruction(ulong offset, int delta, out ulong nearOffset);
        int GetStackTrace(ulong frameOffset, ulong stackOffset, ulong instructionOffset, nint frames, uint size, out uint filled);
        int GetReturnOffset(out ulong ret);
        int OutputStackTrace(uint outputControl, nint frames, uint size, uint flags);
        int GetDebuggeeType(out uint cls, out uint qual);
        int GetActualProcessorType(out uint type);
        int Execute(uint outputControl, string command, uint flags); // Execute command
                                                                     // 后面方法继续，这里裁剪
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("67721FE9-56D2-4A44-A325-2B65513CE6EB")]
    private interface IDebugOutputCallbacksWide
    {
        // mask: DEBUG_OUTPUT_*
        [PreserveSig]
        int Output(uint mask, [MarshalAs(UnmanagedType.LPWStr)] string text);
    }

    private class OutputCapture : IDebugOutputCallbacksWide
    {
        private readonly StringBuilder _sb = new();
        public int Output(uint mask, string text)
        {
            _sb.Append(text);
            return 0; // S_OK
        }
        public override string ToString() => _sb.ToString();
    }

    public static string RunDumpCommands(string dumpPath, params string[] commands)
    {
        if (!System.IO.File.Exists(dumpPath))
            throw new ArgumentFileNotFoundException(dumpPath);

        // 创建 IDebugClient
        int hr = DebugCreate(ref IID_IDebugClient, out var clientPtr);
        if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        var client = (IDebugClient) Marshal.GetObjectForIUnknown(clientPtr);

        // 打开 dump（OpenDumpFileWide 在更高版本接口里，这里用 ANSI 简化）
        hr = client.OpenDumpFile(dumpPath);
        if (hr != 0) Marshal.ThrowExceptionForHR(hr);

        // 获取 IDebugControl
        hr = DebugCreate(ref IID_IDebugControl, out var controlPtr);
        if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        var control = (IDebugControl) Marshal.GetObjectForIUnknown(controlPtr);

        // 绑定输出回调
        var cap = new OutputCapture();
        client.SetOutputCallbacks(cap);

        // WaitForEvent 通常需要，但这里利用 Execute 前默认已处理加载（简化：实际可调用 control.WaitForEvent(timeout)）
        foreach (var cmd in commands)
        {
            hr = control.Execute(0, cmd, 0);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }

        client.EndSession(0);
        return cap.ToString();
    }

    private sealed class ArgumentFileNotFoundException : Exception
    {
        public ArgumentFileNotFoundException(string path) : base($"Dump file not found: {path}") { }
    }
}
