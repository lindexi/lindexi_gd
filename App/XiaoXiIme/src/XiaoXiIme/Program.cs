using System.Runtime.InteropServices;

namespace XiaoXiIme;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var openProductUi = args is ["ui", ..];
        var useUi = args.Length == 0 || openProductUi || args[0] is "--experience" or "--dictionary-debug";
        if (!useUi)
        {
            AttachConsole(uint.MaxValue);
        }

        var root = AppContext.BaseDirectory;
        var dotnetRoot = Path.Combine(root, "dotnet");
        var assemblyPath = useUi
            ? Path.Combine(root, "Lib", "ImeUI", "XiaoXiIme.ImeUi.Avalonia.dll")
            : Path.Combine(root, "Lib", "Cli", "XiaoXiIme.Cli.dll");
        string[] applicationArgs = openProductUi ? ["--serve", .. args.AsSpan(1)] : args;

        try
        {
            var hostDirectory = Path.Combine(dotnetRoot, "host", "fxr");
            var providers = Directory.GetDirectories(hostDirectory);
            if (providers.Length != 1)
            {
                Console.Error.WriteLine($"[Launcher][LocateRuntime] Directory={hostDirectory}; HostFxrDirectoryCount={providers.Length}; Expected=1");
                return 1;
            }

            var hostPath = Path.Combine(providers[0], "hostfxr.dll");
            if (!File.Exists(assemblyPath))
            {
                Console.Error.WriteLine($"[Launcher][LocateApplication] AssemblyPath={assemblyPath}; Error=File not found");
                return 1;
            }

            return RunApplication(hostPath, dotnetRoot, assemblyPath, applicationArgs);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DllNotFoundException
                                       or BadImageFormatException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"[Launcher][Start] DotnetRoot={dotnetRoot}; AssemblyPath={assemblyPath}; Error={error}");
            return 1;
        }
    }

    private static unsafe int RunApplication(string hostPath, string dotnetRoot, string assemblyPath, string[] args)
    {
        var library = NativeLibrary.Load(hostPath);
        try
        {
            var initialize = (delegate* unmanaged[Cdecl]<int, nint*, HostParameters*, nint*, int>)
                NativeLibrary.GetExport(library, "hostfxr_initialize_for_dotnet_command_line");
            var run = (delegate* unmanaged[Cdecl]<nint, int>) NativeLibrary.GetExport(library, "hostfxr_run_app");
            var close = (delegate* unmanaged[Cdecl]<nint, int>) NativeLibrary.GetExport(library, "hostfxr_close");
            var argv = new nint[args.Length + 1];
            try
            {
                argv[0] = Marshal.StringToCoTaskMemUni(assemblyPath);
                for (var index = 0; index < args.Length; index++)
                {
                    argv[index + 1] = Marshal.StringToCoTaskMemUni(args[index]);
                }

                var executablePath = Environment.ProcessPath;
                fixed (char* host = executablePath, runtime = dotnetRoot)
                fixed (nint* arguments = argv)
                {
                    var parameters = new HostParameters { Size = (nuint) sizeof(HostParameters), HostPath = host, DotnetRoot = runtime };
                    nint context = 0;
                    var status = initialize(argv.Length, arguments, &parameters, &context);
                    if (status < 0 || context == 0)
                    {
                        Console.Error.WriteLine($"[Launcher][InitializeRuntime] HostPath={hostPath}; AssemblyPath={assemblyPath}; Status=0x{status:X8}");
                        return status < 0 ? status : 1;
                    }

                    try
                    {
                        var result = run(context);
                        if (result < 0)
                        {
                            Console.Error.WriteLine($"[Launcher][RunApplication] AssemblyPath={assemblyPath}; Result=0x{result:X8}");
                        }

                        return result;
                    }
                    finally
                    {
                        close(context);
                    }
                }
            }
            finally
            {
                foreach (var argument in argv)
                {
                    Marshal.FreeCoTaskMem(argument);
                }
            }
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern int AttachConsole(uint processId);

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct HostParameters
    {
        internal nuint Size;
        internal char* HostPath;
        internal char* DotnetRoot;
    }
}
