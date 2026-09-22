using System.Runtime.Hosting.Native;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using DotNetHost = System.Runtime.Hosting.Native.PInvoke;
using PInvoke = Windows.Win32.PInvoke;

internal static class HostFxrRunner
{
    internal static unsafe int Run(
        string runtimeConfigPath,
        string assemblyPath,
        string typeName,
        string methodName,
        string delegateTypeName,
        string[] args)
    {
        const int PathMax = 260;
        char* hostFxrPath = stackalloc char[PathMax];
        nuint hostFxrPathSize = PathMax;
        int result = DotNetHost.get_hostfxr_path(hostFxrPath, &hostFxrPathSize);
        ThrowIfFailed(result, "Locating hostfxr failed.");

        using FreeLibrarySafeHandle library = new(PInvoke.LoadLibrary(new PCWSTR(hostFxrPath)), true);
        if (library.IsInvalid)
        {
            throw new InvalidOperationException("Loading hostfxr failed.");
        }

        hostfxr_initialize_for_runtime_config_fn initialize = GetExport<hostfxr_initialize_for_runtime_config_fn>(library, "hostfxr_initialize_for_runtime_config");
        hostfxr_get_runtime_delegate_fn getRuntimeDelegate = GetExport<hostfxr_get_runtime_delegate_fn>(library, "hostfxr_get_runtime_delegate");
        hostfxr_close_fn close = GetExport<hostfxr_close_fn>(library, "hostfxr_close");

        hostfxr_handle context = default;
        fixed (char* runtimeConfigPathPointer = runtimeConfigPath)
        {
            result = initialize(runtimeConfigPathPointer, null, &context);
        }

        ThrowIfFailed(result, "Initializing the .NET runtime failed.");
        if (context.IsNull)
        {
            throw new InvalidOperationException("hostfxr returned an empty runtime context.");
        }

        try
        {
            FARPROC loadAssemblyPointer = default;
            result = getRuntimeDelegate(
                context,
                hostfxr_delegate_type.hdt_load_assembly_and_get_function_pointer,
                &loadAssemblyPointer);
            ThrowIfFailed(result, "Getting the assembly loader failed.");

            load_assembly_and_get_function_pointer_fn loadAssembly = loadAssemblyPointer.CreateDelegate<load_assembly_and_get_function_pointer_fn>();
            FARPROC entryPointPointer = default;
            fixed (char* assemblyPathPointer = assemblyPath)
            fixed (char* typeNamePointer = typeName)
            fixed (char* methodNamePointer = methodName)
            fixed (char* delegateTypeNamePointer = delegateTypeName)
            {
                result = loadAssembly(
                    assemblyPathPointer,
                    typeNamePointer,
                    methodNamePointer,
                    delegateTypeNamePointer,
                    null,
                    &entryPointPointer);
            }

            ThrowIfFailed(result, "Loading the managed entry point failed.");
            ComponentEntryPoint entryPoint = entryPointPointer.CreateDelegate<ComponentEntryPoint>();
            return entryPoint(args);
        }
        finally
        {
            result = close(context);
            ThrowIfFailed(result, "Closing the hostfxr context failed.");
        }
    }

    private static TDelegate GetExport<TDelegate>(FreeLibrarySafeHandle library, string name)
        where TDelegate : Delegate
    {
        FARPROC address = PInvoke.GetProcAddress(library, name);
        if (address.IsNull)
        {
            throw new MissingMethodException($"The hostfxr export '{name}' was not found.");
        }

        return address.CreateDelegate<TDelegate>();
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} hostfxr result: 0x{result:X8}.");
        }
    }
}
