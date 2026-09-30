using System.Runtime.InteropServices;
using Microsoft.Win32;

return await TsfAbiHost.RunAsync(args);

internal static unsafe class TsfAbiHost
{
    private const int S_OK = 0;
    private const int S_FALSE = 1;
    private const int E_NOINTERFACE = unchecked((int) 0x80004002);
    private const int CLASS_E_NOAGGREGATION = unchecked((int) 0x80040110);
    private static readonly Guid ClassId = new("8B9C319B-132F-4931-9248-DFC740920F52");
    private static readonly Guid IUnknownId = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IClassFactoryId = new("00000001-0000-0000-C000-000000000046");

    public static Task<int> RunAsync(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("The TSF ABI host requires Windows.");
            return Task.FromResult(2);
        }

        var command = args.FirstOrDefault() ?? "help";
        var libraryPath = args.Skip(1).FirstOrDefault();
        if (command is "abi" or "com-activation" && string.IsNullOrWhiteSpace(libraryPath))
        {
            Console.Error.WriteLine($"{command} requires the full path to XiaoXiIme.TsfModule.dll.");
            return Task.FromResult(2);
        }

        try
        {
            return Task.FromResult(command switch
            {
                "abi" => RunAbi(Path.GetFullPath(libraryPath!)),
                "com-activation" => RunComActivation(Path.GetFullPath(libraryPath!)),
                _ => PrintHelp(),
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Task.FromResult(1);
        }
    }

    private static int RunAbi(string libraryPath)
    {
        var module = NativeLibrary.Load(libraryPath);
        var getClassObject = (delegate* unmanaged[Stdcall]<Guid*, Guid*, nint*, int>) NativeLibrary.GetExport(module, "DllGetClassObject");
        var canUnloadNow = (delegate* unmanaged[Stdcall]<int>) NativeLibrary.GetExport(module, "DllCanUnloadNow");

        Require(canUnloadNow() == S_FALSE, "NativeAOT DLL must remain loaded.");
        var classId = ClassId;
        var factoryId = IClassFactoryId;
        nint factory = 0;
        Require(getClassObject(&classId, &factoryId, &factory) == S_OK && factory != 0, "DllGetClassObject did not return IClassFactory.");

        var vtable = *(void***) factory;
        var queryInterface = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>) vtable[0];
        var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[2];
        var createInstance = (delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, int>) vtable[3];
        var lockServer = (delegate* unmanaged[Stdcall]<nint, int, int>) vtable[4];
        var unknownId = IUnknownId;
        nint unknown = 0;
        Require(queryInterface(factory, &unknownId, &unknown) == S_OK && unknown == factory, "IUnknown identity mismatch.");
        Require(release(unknown) == 1, "QueryInterface did not AddRef exactly once.");

        nint created = 42;
        Require(createInstance(factory, 1, &unknownId, &created) == CLASS_E_NOAGGREGATION && created == 0, "Aggregation contract failed.");
        created = 42;
        Require(createInstance(factory, 0, &unknownId, &created) == E_NOINTERFACE && created == 0, "Unsupported coclass contract failed.");
        Require(lockServer(factory, 1) == S_OK && lockServer(factory, 0) == S_OK, "LockServer contract failed.");
        Require(release(factory) == 0, "Final Release did not destroy the class factory.");
        Console.WriteLine("TSF ABI/vtable contract passed.");
        return 0;
    }

    private static int RunComActivation(string libraryPath)
    {
        using var registration = PerUserComRegistration.Create(ClassId, libraryPath);
        var initializeResult = CoInitializeEx(0, 0);
        Require(initializeResult >= 0, $"CoInitializeEx failed: 0x{initializeResult:X8}");
        try
        {
            var classId = ClassId;
            var factoryId = IClassFactoryId;
            var hr = CoGetClassObject(&classId, 1, 0, &factoryId, out var factory);
            Require(hr == S_OK && factory != 0, $"CoGetClassObject failed: 0x{hr:X8}");
            var vtable = *(void***) factory;
            var release = (delegate* unmanaged[Stdcall]<nint, uint>) vtable[2];
            release(factory);
            Console.WriteLine("Isolated COM activation passed.");
            return 0;
        }
        finally
        {
            CoUninitialize();
        }
    }

    private static int PrintHelp()
    {
        Console.WriteLine("XiaoXiIme.TsfAbiHost abi <native-aot-dll>");
        Console.WriteLine("XiaoXiIme.TsfAbiHost com-activation <native-aot-dll>");
        return 0;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(nint reserved, uint coInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [DllImport("ole32.dll")]
    private static extern int CoGetClassObject(Guid* classId, uint context, nint serverInfo, Guid* interfaceId, out nint result);
}

internal sealed class PerUserComRegistration : IDisposable
{
    private readonly string _classKeyPath;

    private PerUserComRegistration(string classKeyPath)
    {
        _classKeyPath = classKeyPath;
    }

    public static PerUserComRegistration Create(Guid classId, string libraryPath)
    {
        var classKeyPath = $"Software\\Classes\\CLSID\\{{{classId:D}}}";
        Registry.CurrentUser.DeleteSubKeyTree(classKeyPath, false);
        using var classKey = Registry.CurrentUser.CreateSubKey(classKeyPath, true)
            ?? throw new InvalidOperationException("Unable to create the per-user COM class key.");
        using var serverKey = classKey.CreateSubKey("InprocServer32", true)
            ?? throw new InvalidOperationException("Unable to create InprocServer32.");
        serverKey.SetValue(null, libraryPath, RegistryValueKind.String);
        serverKey.SetValue("ThreadingModel", "Apartment", RegistryValueKind.String);
        return new PerUserComRegistration(classKeyPath);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_classKeyPath, false);
    }
}
