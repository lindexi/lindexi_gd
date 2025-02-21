using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace JenawneekacelFarweardelheeder;

// copy from https://github.com/lowleveldesign/process-governor 
public static class JobProvider
{
    public static unsafe void StartWithMemoryLimit(ulong maxWorkingSetMemory)
    {
        var jobName = $"Jenawneekacel-{Guid.NewGuid():D}";
        var securityAttributes = new SECURITY_ATTRIBUTES();
        securityAttributes.nLength = (uint) Marshal.SizeOf(securityAttributes);
        SafeFileHandle jobHandle = PInvoke.CreateJobObject(securityAttributes, $"Local\\{jobName}");

        var limitInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        var size = (uint) Marshal.SizeOf(limitInfo);

        uint length = 0u;

        PInvoke.QueryInformationJobObject(jobHandle,
            JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, &limitInfo, size, &length);
        Debug.Assert(length == size);

        JOB_OBJECT_LIMIT
            flags = limitInfo.BasicLimitInformation.LimitFlags //& ~JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_AFFINITY;
            ;

        // 内存有 Process Memory 进程内存
        // Job 的内存
        // 工作集（约等于物理内存）

        // 限制内存
        flags |= JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_WORKINGSET;
        limitInfo.BasicLimitInformation.MaximumWorkingSetSize = checked((nuint) maxWorkingSetMemory);

        limitInfo.BasicLimitInformation.LimitFlags = flags;
        PInvoke.SetInformationJobObject(jobHandle,
            JOBOBJECTINFOCLASS.JobObjectBasicLimitInformation,
            &limitInfo, size);

        using Process currentProcess = Process.GetCurrentProcess();
        PInvoke.AssignProcessToJobObject(jobHandle, currentProcess.SafeHandle);
        return;

        var selfApp = Environment.ProcessPath!;
        // 创建进程
        var procThreadAttrList = new LPPROC_THREAD_ATTRIBUTE_LIST();
        // 第一遍获取大小
        nuint procThreadAttrListSize = 0;
        PInvoke.InitializeProcThreadAttributeList(procThreadAttrList, 1, 0, &procThreadAttrListSize);
        var procThreadAttrListPtr = (void*) Marshal.AllocHGlobal((nint) procThreadAttrListSize);
        procThreadAttrList = new LPPROC_THREAD_ATTRIBUTE_LIST(procThreadAttrListPtr);
        // 填充内容
        PInvoke.InitializeProcThreadAttributeList(procThreadAttrList, 1, 0, &procThreadAttrListSize);

        var rawJobHandle = jobHandle.DangerousGetHandle();
        PInvoke.UpdateProcThreadAttribute(procThreadAttrList, 0, PInvoke.PROC_THREAD_ATTRIBUTE_JOB_LIST, &rawJobHandle,
            (nuint)Marshal.SizeOf(rawJobHandle), null, (nuint*)null);

        var startupInfo = new STARTUPINFOEXW()
        {
            StartupInfo = new STARTUPINFOW() { cb = (uint) sizeof(STARTUPINFOEXW) },
            lpAttributeList = procThreadAttrList
        };
        var processCreationFlags = PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;

        var processInfo = new PROCESS_INFORMATION();

        var commandLine = $"\"{selfApp}\"";
        fixed (char* commandLinePtr = commandLine)
        {
            PInvoke.CreateProcess(null, new PWSTR(commandLinePtr), null, null, true, processCreationFlags, null, null,
                (STARTUPINFOW*)&startupInfo, &processInfo);
        }

        PInvoke.DeleteProcThreadAttributeList(procThreadAttrList);
        Marshal.FreeHGlobal((nint)procThreadAttrListPtr);

        Environment.Exit(0);
    }
}