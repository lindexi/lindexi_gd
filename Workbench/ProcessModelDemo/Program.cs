#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0-windows
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property PublishAot=false

// Run:
//   dotnet run --file Program.cs
//
// Or supply a command line:
//   dotnet run --file Program.cs -- "cmd.exe /c whoami"
//
// What this file demonstrates:
//   1. Probe the PSEC API-set.
//   2. Load %SystemRoot%\System32\processmodel.dll securely.
//   3. Resolve PSEC exports dynamically.
//   4. Query PSEC schema/capability support.
//   5. Build a minimal valid PSEC v1.0 FlatBuffer ("PSEC" identifier).
//   6. Create an HPROCESS_SECURITY_ENVIRONMENT.
//   7. Start a suspended process with
//      PROC_THREAD_ATTRIBUTE_SECURITY_ENVIRONMENT.
//   8. Assign the child to a Job Object before it runs.
//   9. Resume, wait, timeout-kill the complete process tree.
//  10. Close PSEC after the child tree has stopped.
//
// IMPORTANT:
// - processmodel.dll / PSEC is feature-gated and unavailable on many hosts.
// - A successful PSEC API probe does NOT imply every policy field is supported.
// - This minimal PSEC buffer intentionally has no filesystem grants. A target
//   program may therefore fail to start or access dependencies.
// - For production use, generate C# FlatBuffers bindings from MXC's
//   ProcessSecurityEnvironment.fbs and build a complete policy.
// - Never silently fall back to a normal unrestricted CreateProcessW call when
//   a policy requires PSEC enforcement.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

return PsecDirectDemo.Run(args);

internal static class PsecDirectDemo
{
    // MXC probes this API-set before loading processmodel.dll.
    private const string SecurityEnvironmentApiSet =
        "api-win-appmodel-processmodel~securityenvironment";

    // PSEC schema version used by this minimal example.
    private const ushort PsecMajor = 1;
    private const ushort PsecMinor = 0;

    // Feature bits used by MXC's QueryProcessSecurityEnvironmentSupport wrapper.
    private const ulong PsecSupportFileSystemDeny = 0x0000_0000_0000_0001;
    private const ulong PsecSupportFileSystemEnumerate = 0x0000_0000_0000_0004;
    private const ulong PsecSupportNetworkIngress = 0x0000_0000_0000_0008;

    // CreateProcess flags.
    private const uint CREATE_SUSPENDED = 0x00000004;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const uint CREATE_NO_WINDOW = 0x08000000;

    // Job Object constants.
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
    private const int JobObjectExtendedLimitInformation = 9;

    // Wait constants.
    private const uint WAIT_OBJECT_0 = 0x00000000;
    private const uint WAIT_TIMEOUT = 0x00000102;
    private const uint INFINITE = 0xFFFFFFFF;

    // MXC defines this as:
    // const PROC_THREAD_ATTRIBUTE_SECURITY_ENVIRONMENT: usize = 35 | 0x0002_0000;
    private static readonly nuint ProcThreadAttributeSecurityEnvironment =
        (nuint)(35 | 0x0002_0000);

    public static int Run(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("This file-based app requires Windows.");
            return 2;
        }

        IntPtr processModel = IntPtr.Zero;
        IntPtr psec = IntPtr.Zero;
        IntPtr job = IntPtr.Zero;
        IntPtr attributeList = IntPtr.Zero;
        IntPtr psecHandleStorage = IntPtr.Zero;
        IntPtr environmentBlock = IntPtr.Zero;
        PROCESS_INFORMATION child = default;

        try
        {
            // 1. PSEC API-set probe.
            if (!TryIsApiSetImplemented(
                    SecurityEnvironmentApiSet,
                    out bool apiSetAvailable))
            {
                Console.WriteLine(
                    "Warning: IsApiSetImplemented could not be resolved; " +
                    "continuing with direct DLL/export probing.");
            }
            else if (!apiSetAvailable)
            {
                Console.Error.WriteLine(
                    $"PSEC API-set is unavailable: {SecurityEnvironmentApiSet}");
                return 3;
            }

            // 2. Load only from System32; do not use an unqualified LoadLibrary.
            processModel = Native.LoadLibraryExW(
                "processmodel.dll",
                IntPtr.Zero,
                Native.LOAD_LIBRARY_SEARCH_SYSTEM32);

            if (processModel == IntPtr.Zero)
            {
                throw LastWin32Error("LoadLibraryExW(processmodel.dll)");
            }

            var api = ProcessSecurityEnvironmentApi.Load(processModel);

            // 3. Probe PSEC schema and feature support.
            bool supportsPsec10 = api.SupportsVersion(major: 1, requiredMinor: 0);
            bool supportsPsec11 = api.SupportsVersion(major: 1, requiredMinor: 1);
            ulong supportFlags = api.QuerySupportFlags();

            Console.WriteLine($"PSEC schema 1.0 supported: {supportsPsec10}");
            Console.WriteLine($"PSEC schema 1.1 supported: {supportsPsec11}");
            Console.WriteLine($"PSEC support flags: 0x{supportFlags:X16}");
            Console.WriteLine(
                $"  filesystem deny:      {(supportFlags & PsecSupportFileSystemDeny) != 0}");
            Console.WriteLine(
                $"  filesystem enumerate: {(supportFlags & PsecSupportFileSystemEnumerate) != 0}");
            Console.WriteLine(
                $"  network ingress:      {(supportFlags & PsecSupportNetworkIngress) != 0}");

            if (!supportsPsec10)
            {
                Console.Error.WriteLine(
                    "PSEC schema version 1.0 is unsupported on this host.");
                return 4;
            }

            // 4. Build a PSEC v1.0 FlatBuffer.
            //
            // Policy represented here:
            // - version: 1.0
            // - no explicit AppContainer capabilities
            // - no filesystem grants
            // - no fs_deny/fs_enumerate
            // - no UI policy
            // - egress default: deny
            //
            // The PSEC file identifier is "PSEC".
            byte[] psecBlob = BuildMinimalPsecV10DenyEgress();

            Console.WriteLine(
                $"PSEC FlatBuffer created: {psecBlob.Length} bytes, " +
                $"identifier={Encoding.ASCII.GetString(psecBlob, 4, 4)}");

            // 5. Create the process security environment.
            int createHr = api.Create(
                psecBlob,
                flags: 0, // PROCESS_SECURITY_ENVIRONMENT_FLAG_NONE
                out psec);

            ThrowIfFailed(createHr, "CreateProcessSecurityEnvironment");

            if (psec == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "CreateProcessSecurityEnvironment returned a null handle.");
            }

            // 6. Build the extended attribute list.
            attributeList = CreateAttributeList(attributeCount: 1);

            // UpdateProcThreadAttribute receives a pointer to the HANDLE value.
            psecHandleStorage = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(psecHandleStorage, psec);

            if (!Native.UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    ProcThreadAttributeSecurityEnvironment,
                    psecHandleStorage,
                    (nuint)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero))
            {
                throw LastWin32Error(
                    "UpdateProcThreadAttribute(SecurityEnvironment)");
            }

            // 7. Supply an explicit, small child environment.
            //
            // Passing null would inherit all environment variables from this
            // launcher process, which is normally undesirable for sandboxing.
            environmentBlock = Marshal.StringToHGlobalUni(
                BuildMinimalEnvironmentBlock());

            string commandLine = args.Length == 0
                ? DefaultCommandLine()
                : string.Join(' ', args);

            Console.WriteLine($"Command line: {commandLine}");

            var startup = new STARTUPINFOEX();
            startup.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            startup.lpAttributeList = attributeList;

            // Create suspended first. The child must join the Job Object before
            // its first user-mode instruction is allowed to run.
            uint creationFlags =
                CREATE_SUSPENDED |
                CREATE_UNICODE_ENVIRONMENT |
                CREATE_NO_WINDOW |
                EXTENDED_STARTUPINFO_PRESENT;

            var mutableCommandLine = new StringBuilder(commandLine);

            bool created = Native.CreateProcessW(
                lpApplicationName: null,
                lpCommandLine: mutableCommandLine,
                lpProcessAttributes: IntPtr.Zero,
                lpThreadAttributes: IntPtr.Zero,
                bInheritHandles: false,
                dwCreationFlags: creationFlags,
                lpEnvironment: environmentBlock,
                lpCurrentDirectory: Environment.CurrentDirectory,
                lpStartupInfo: ref startup,
                lpProcessInformation: out child);

            if (!created)
            {
                throw LastWin32Error(
                    "CreateProcessW(PROC_THREAD_ATTRIBUTE_SECURITY_ENVIRONMENT)");
            }

            Console.WriteLine(
                $"Created suspended PSEC child: PID={child.dwProcessId}");

            // 8. Create a Job Object for process-tree lifetime control.
            job = Native.CreateJobObjectW(IntPtr.Zero, null);

            if (job == IntPtr.Zero)
            {
                TerminateAndReapRoot(child.hProcess);
                throw LastWin32Error("CreateJobObjectW");
            }

            var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            limits.BasicLimitInformation.LimitFlags =
                JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            if (!Native.SetInformationJobObject(
                    job,
                    JobObjectExtendedLimitInformation,
                    ref limits,
                    (uint)Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
            {
                TerminateAndReapRoot(child.hProcess);
                throw LastWin32Error(
                    "SetInformationJobObject(JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE)");
            }

            // Fail closed: do not execute a sandbox process if it cannot be
            // placed in the Job Object that controls its descendants.
            if (!Native.AssignProcessToJobObject(job, child.hProcess))
            {
                TerminateAndReapRoot(child.hProcess);
                throw LastWin32Error("AssignProcessToJobObject");
            }

            // 9. Let the child execute only after Job assignment.
            uint resumeResult = Native.ResumeThread(child.hThread);

            if (resumeResult == uint.MaxValue)
            {
                Native.TerminateJobObject(job, unchecked((uint)-1));
                Native.WaitForSingleObject(child.hProcess, INFINITE);
                throw LastWin32Error("ResumeThread");
            }

            Console.WriteLine("Child resumed.");

            // 10. Wait for the root process. Timeout terminates the whole Job.
            const uint timeoutMs = 30_000;

            uint waitResult = Native.WaitForSingleObject(
                child.hProcess,
                timeoutMs);

            if (waitResult == WAIT_TIMEOUT)
            {
                Console.Error.WriteLine(
                    $"Timed out after {timeoutMs} ms; terminating the Job tree.");

                if (!Native.TerminateJobObject(job, unchecked((uint)-1)))
                {
                    throw LastWin32Error("TerminateJobObject");
                }

                Native.WaitForSingleObject(child.hProcess, INFINITE);
                return 124;
            }

            if (waitResult != WAIT_OBJECT_0)
            {
                throw LastWin32Error("WaitForSingleObject");
            }

            if (!Native.GetExitCodeProcess(child.hProcess, out uint exitCode))
            {
                throw LastWin32Error("GetExitCodeProcess");
            }

            Console.WriteLine($"Child exit code: {exitCode}");

            // The root has exited. Terminate the Job so background descendants,
            // if any, are also stopped before PSEC teardown.
            Native.TerminateJobObject(job, unchecked((uint)-1));
            Native.WaitForSingleObject(child.hProcess, INFINITE);

            return unchecked((int)exitCode);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"PSEC launch failed: {error.Message}");

            // Best-effort cleanup. Prefer Job termination because it kills
            // the root and all descendants.
            if (job != IntPtr.Zero)
            {
                Native.TerminateJobObject(job, unchecked((uint)-1));
            }
            else if (child.hProcess != IntPtr.Zero)
            {
                Native.TerminateProcess(child.hProcess, unchecked((uint)-1));
            }

            return 1;
        }
        finally
        {
            // Security environment must outlive the child process tree.
            if (child.hProcess != IntPtr.Zero)
            {
                Native.WaitForSingleObject(child.hProcess, INFINITE);
            }

            if (child.hThread != IntPtr.Zero)
            {
                Native.CloseHandle(child.hThread);
            }

            if (child.hProcess != IntPtr.Zero)
            {
                Native.CloseHandle(child.hProcess);
            }

            if (job != IntPtr.Zero)
            {
                Native.CloseHandle(job);
            }

            if (attributeList != IntPtr.Zero)
            {
                Native.DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
            }

            if (psecHandleStorage != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(psecHandleStorage);
            }

            if (environmentBlock != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(environmentBlock);
            }

            if (psec != IntPtr.Zero && processModel != IntPtr.Zero)
            {
                try
                {
                    ProcessSecurityEnvironmentApi.Load(processModel).Close(psec);
                }
                catch
                {
                    // Preserve the original exception/result.
                }
            }

            // Intentionally do not FreeLibrary(processModel).
            //
            // This follows MXC's approach: processmodel.dll is treated as a
            // resident system DLL, and the resolved delegate pointers remain
            // valid during this process's lifetime.
        }
    }

    private static string DefaultCommandLine()
    {
        string comSpec =
            Environment.GetEnvironmentVariable("ComSpec")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32",
                "cmd.exe");

        return $"\"{comSpec}\" /c echo Hello from PSEC";
    }

    private static string BuildMinimalEnvironmentBlock()
    {
        string windows = Environment.GetFolderPath(
            Environment.SpecialFolder.Windows);

        string system32 = Path.Combine(windows, "System32");
        string drive = Path.GetPathRoot(windows)?.TrimEnd('\\') ?? "C:";
        string temp = Path.GetTempPath().TrimEnd('\\');

        // Windows requires a double-NUL-terminated environment block.
        return string.Join(
            '\0',
            $"SystemRoot={windows}",
            $"windir={windows}",
            $"SystemDrive={drive}",
            $"ComSpec={Path.Combine(system32, "cmd.exe")}",
            $"PATH={system32}",
            $"TEMP={temp}",
            $"TMP={temp}") + "\0\0";
    }

    private static IntPtr CreateAttributeList(int attributeCount)
    {
        nuint bytes = 0;

        // This sizing call is expected to fail with ERROR_INSUFFICIENT_BUFFER.
        bool sizingCall = Native.InitializeProcThreadAttributeList(
            IntPtr.Zero,
            attributeCount,
            0,
            ref bytes);

        int sizingError = Marshal.GetLastWin32Error();

        if (sizingCall ||
            sizingError != Native.ERROR_INSUFFICIENT_BUFFER ||
            bytes == 0)
        {
            throw new Win32Exception(
                sizingError,
                "InitializeProcThreadAttributeList sizing call failed.");
        }

        IntPtr list = Marshal.AllocHGlobal(checked((int)bytes));

        if (!Native.InitializeProcThreadAttributeList(
                list,
                attributeCount,
                0,
                ref bytes))
        {
            int error = Marshal.GetLastWin32Error();
            Marshal.FreeHGlobal(list);

            throw new Win32Exception(
                error,
                "InitializeProcThreadAttributeList failed.");
        }

        return list;
    }

    private static void TerminateAndReapRoot(IntPtr process)
    {
        if (process == IntPtr.Zero)
        {
            return;
        }

        Native.TerminateProcess(process, unchecked((uint)-1));
        Native.WaitForSingleObject(process, INFINITE);
    }

    /// <summary>
    /// Creates a minimal valid PSEC v1.0 FlatBuffer manually.
    ///
    /// MXC schema source:
    /// external/windows-sdk/ProcessSecurityEnvironment.fbs
    ///
    /// Encoded fields:
    /// - ProcessSecurityEnvironment.version = { major = 1, minor = 0 }
    /// - ProcessSecurityEnvironment.network_policy.egress.default_action = deny
    ///
    /// The FlatBuffer identifier at bytes [4..7] is "PSEC".
    ///
    /// This deliberately omits capabilities, filesystem path vectors,
    /// fs_deny, fs_enumerate, proxy and ingress fields.
    /// </summary>
    private static byte[] BuildMinimalPsecV10DenyEgress()
    {
        // FlatBuffers are little-endian.
        //
        // Byte layout:
        //
        //   0..3   root table uoffset
        //   4..7   file identifier "PSEC"
        //   8..29  root table vtable
        //   32..59 root ProcessSecurityEnvironment table
        //   60..71 NetworkPolicy vtable
        //   72..79 NetworkPolicy table
        //   80..85 EndpointPolicy vtable
        //   88..95 EndpointPolicy table
        //
        // This reflects these PSEC schema fields:
        //
        // ProcessSecurityEnvironment:
        //   field 0: version
        //   field 7: network_policy
        //
        // NetworkPolicy:
        //   field 1: egress
        //
        // EndpointPolicy:
        //   field 0: default_action = deny (enum value 0)

        byte[] buffer = new byte[96];

        // Root object starts at byte 32.
        WriteUInt32(buffer, 0, 32);

        // FlatBuffers file identifier from ProcessSecurityEnvironment.fbs.
        buffer[4] = (byte)'P';
        buffer[5] = (byte)'S';
        buffer[6] = (byte)'E';
        buffer[7] = (byte)'C';

        // Root vtable at byte 8:
        // 4 bytes header + 9 root fields * 2 bytes = 22 bytes.
        WriteUInt16(buffer, 8, 22);  // vtable size
        WriteUInt16(buffer, 10, 28); // object size

        // Root field 0 (version) is at object offset 4.
        WriteUInt16(buffer, 12 + (0 * 2), 4);

        // Root field 7 (network_policy) is at object offset 24.
        WriteUInt16(buffer, 12 + (7 * 2), 24);

        // Root table at byte 32; vtable starts 24 bytes earlier.
        WriteInt32(buffer, 32, 24);

        // Root field 0: SchemaVersion struct, major/minor.
        WriteUInt16(buffer, 36, PsecMajor);
        WriteUInt16(buffer, 38, PsecMinor);

        // Root field 7 at byte 56:
        // NetworkPolicy table starts at 72, so relative uoffset = 16.
        WriteUInt32(buffer, 56, 16);

        // NetworkPolicy vtable at byte 60:
        // 4 bytes header + 4 fields * 2 bytes = 12 bytes.
        WriteUInt16(buffer, 60, 12); // vtable size
        WriteUInt16(buffer, 62, 8);  // object size

        // NetworkPolicy field 1 (egress) is at object offset 4.
        WriteUInt16(buffer, 64 + (1 * 2), 4);

        // NetworkPolicy table at byte 72; vtable is 12 bytes back.
        WriteInt32(buffer, 72, 12);

        // Egress EndpointPolicy begins at byte 88:
        // relative to uoffset field at byte 76 => 12.
        WriteUInt32(buffer, 76, 12);

        // EndpointPolicy vtable:
        // 4 bytes header + 1 field * 2 bytes = 6 bytes.
        WriteUInt16(buffer, 80, 6); // vtable size
        WriteUInt16(buffer, 82, 8); // object size

        // EndpointPolicy field 0 default_action at object offset 4.
        WriteUInt16(buffer, 84, 4);

        // EndpointPolicy table at byte 88; vtable is 8 bytes back.
        WriteInt32(buffer, 88, 8);

        // buffer[92] = default_action = deny = 0.
        buffer[92] = 0;

        return buffer;
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteInt32(byte[] buffer, int offset, int value) =>
        WriteUInt32(buffer, offset, unchecked((uint)value));

    private static bool TryIsApiSetImplemented(
        string contract,
        out bool implemented)
    {
        try
        {
            implemented = Native.IsApiSetImplemented(contract);
            return true;
        }
        catch (DllNotFoundException)
        {
            implemented = false;
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            implemented = false;
            return false;
        }
    }

    private static void ThrowIfFailed(int hresult, string operation)
    {
        if (hresult < 0)
        {
            throw new COMException(
                $"{operation} failed with HRESULT 0x{hresult:X8}.",
                hresult);
        }
    }

    private static Win32Exception LastWin32Error(string operation)
    {
        int error = Marshal.GetLastWin32Error();
        return new Win32Exception(error, $"{operation} failed.");
    }

    private sealed class ProcessSecurityEnvironmentApi
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int CreateProcessSecurityEnvironmentDelegate(
            IntPtr sandboxSpecification,
            uint sandboxSpecificationSize,
            uint flags,
            out IntPtr processSecurityEnvironment);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int QueryProcessSecurityEnvironmentSupportDelegate(
            out ulong supportFlags);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int IsProcessSecurityEnvironmentVersionSupportedDelegate(
            uint major,
            out byte available,
            out uint highestMinor);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void CloseProcessSecurityEnvironmentDelegate(
            IntPtr processSecurityEnvironment);

        private readonly CreateProcessSecurityEnvironmentDelegate _create;
        private readonly QueryProcessSecurityEnvironmentSupportDelegate _querySupport;
        private readonly IsProcessSecurityEnvironmentVersionSupportedDelegate? _versionSupported;
        private readonly CloseProcessSecurityEnvironmentDelegate _close;

        private ProcessSecurityEnvironmentApi(
            CreateProcessSecurityEnvironmentDelegate create,
            QueryProcessSecurityEnvironmentSupportDelegate querySupport,
            IsProcessSecurityEnvironmentVersionSupportedDelegate? versionSupported,
            CloseProcessSecurityEnvironmentDelegate close)
        {
            _create = create;
            _querySupport = querySupport;
            _versionSupported = versionSupported;
            _close = close;
        }

        public static ProcessSecurityEnvironmentApi Load(IntPtr module)
        {
            IntPtr create = RequireExport(
                module,
                "CreateProcessSecurityEnvironment");

            IntPtr querySupport = RequireExport(
                module,
                "QueryProcessSecurityEnvironmentSupport");

            IntPtr close = RequireExport(
                module,
                "CloseProcessSecurityEnvironment");

            // MXC makes the version query optional and treats a missing export
            // as support for baseline PSEC 1.0 only.
            IntPtr versionSupport = Native.GetProcAddress(
                module,
                "IsProcessSecurityEnvironmentVersionSupported");

            return new ProcessSecurityEnvironmentApi(
                Marshal.GetDelegateForFunctionPointer<
                    CreateProcessSecurityEnvironmentDelegate>(create),
                Marshal.GetDelegateForFunctionPointer<
                    QueryProcessSecurityEnvironmentSupportDelegate>(querySupport),
                versionSupport == IntPtr.Zero
                    ? null
                    : Marshal.GetDelegateForFunctionPointer<
                        IsProcessSecurityEnvironmentVersionSupportedDelegate>(
                        versionSupport),
                Marshal.GetDelegateForFunctionPointer<
                    CloseProcessSecurityEnvironmentDelegate>(close));
        }

        public int Create(
            byte[] specification,
            uint flags,
            out IntPtr processSecurityEnvironment)
        {
            GCHandle pinnedBuffer = GCHandle.Alloc(
                specification,
                GCHandleType.Pinned);

            try
            {
                return _create(
                    pinnedBuffer.AddrOfPinnedObject(),
                    checked((uint)specification.Length),
                    flags,
                    out processSecurityEnvironment);
            }
            finally
            {
                pinnedBuffer.Free();
            }
        }

        public ulong QuerySupportFlags()
        {
            int hr = _querySupport(out ulong flags);
            ThrowIfFailed(hr, "QueryProcessSecurityEnvironmentSupport");
            return flags;
        }

        public bool SupportsVersion(uint major, uint requiredMinor)
        {
            if (_versionSupported is null)
            {
                return major == 1 && requiredMinor == 0;
            }

            int hr = _versionSupported(
                major,
                out byte available,
                out uint highestMinor);

            ThrowIfFailed(
                hr,
                "IsProcessSecurityEnvironmentVersionSupported");

            return available != 0 && highestMinor >= requiredMinor;
        }

        public void Close(IntPtr processSecurityEnvironment) =>
            _close(processSecurityEnvironment);

        private static IntPtr RequireExport(
            IntPtr module,
            string exportName)
        {
            IntPtr export = Native.GetProcAddress(module, exportName);

            if (export == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException(
                    $"processmodel.dll export missing: {exportName}; " +
                    $"GetLastError={Marshal.GetLastWin32Error()}");
            }

            return export;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    private static class Native
    {
        public const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport(
            "api-ms-win-core-apiquery-l2-1-0.dll",
            CharSet = CharSet.Ansi,
            SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsApiSetImplemented(string contract);

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        public static extern IntPtr LoadLibraryExW(
            string lpFileName,
            IntPtr hFile,
            uint dwFlags);

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        public static extern IntPtr GetProcAddress(
            IntPtr hModule,
            string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InitializeProcThreadAttributeList(
            IntPtr lpAttributeList,
            int dwAttributeCount,
            int dwFlags,
            ref nuint lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateProcThreadAttribute(
            IntPtr lpAttributeList,
            uint dwFlags,
            nuint attribute,
            IntPtr lpValue,
            nuint cbSize,
            IntPtr lpPreviousValue,
            IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = false)]
        public static extern void DeleteProcThreadAttributeList(
            IntPtr lpAttributeList);

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessW(
            string? lpApplicationName,
            StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFOEX lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        public static extern IntPtr CreateJobObjectW(
            IntPtr lpJobAttributes,
            string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(
            IntPtr hJob,
            int jobObjectInfoClass,
            ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(
            IntPtr hJob,
            IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateJobObject(
            IntPtr hJob,
            uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(
            IntPtr hProcess,
            uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(
            IntPtr hHandle,
            uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(
            IntPtr hProcess,
            out uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);
    }
}