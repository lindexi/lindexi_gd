using System;
using System.IO;
using System.Runtime.InteropServices;
using HRESULT = System.Int32;
using DWORD = System.UInt32;

namespace DotNetFramework
{
    public unsafe class Program
    {
        public static void Main(string[] args)
        {
            IntPtr libraryHandle = LoadCoreCLR();

            var procAddressHandle = GetProcAddress(libraryHandle, "GetCLRRuntimeHost");
            var procAddress = Marshal.GetDelegateForFunctionPointer<FnGetCLRRuntimeHost>(procAddressHandle);

            IntPtr runtimeHostPtr = default;
            var guid = new Guid(0x64F6D366, 0xD7C2, 0x4F1F, 0xB4, 0xB2, 0xE8, 0x16, 0x0C, 0xAC, 0x43, 0xAF);
            var hResult = procAddress(guid, &runtimeHostPtr);
            Marshal.ThrowExceptionForHR(hResult);

            var runtimeHost = (ICLRRuntimeHost) Marshal.GetObjectForIUnknown(runtimeHostPtr);
            runtimeHost.Start();

        }

        private static IntPtr LoadCoreCLR()
        {
            var programFilesFolder =
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var dotnetFolder = Path.Combine(programFilesFolder, "dotnet");
            var dotnetSharedFolder = Path.Combine(dotnetFolder, "shared");
            var dotnetCoreFolder = Path.Combine(dotnetSharedFolder, "Microsoft.NETCore.App");
            var version = "6.0.0";
            var dotnetVersionFolder = Path.Combine(dotnetCoreFolder, version);

            var coreClrFile = Path.Combine(dotnetVersionFolder, "coreclr.dll");

            var libraryHandle = LoadLibrary(coreClrFile);
            return libraryHandle;
        }

        delegate int FnGetCLRRuntimeHost(Guid hostGuid, IntPtr* runtimeHost);

        [DllImport("kernel32.dll")]
        private extern static IntPtr LoadLibrary(String path);

        [DllImport("kernel32.dll")]
        private extern static IntPtr GetProcAddress(IntPtr lib, string methodName);

        [DllImport("kernel32.dll")]
        private extern static bool FreeLibrary(IntPtr lib);
    }

    [Guid("64f6d366-d7c2-4f1f-b4b2-e8160cac43af")]
    [ComImport()]
    unsafe interface ICLRRuntimeHost
    {
        HRESULT Start();
        HRESULT Stop();
        HRESULT SetHostControl(in IntPtr pHostControl);
        HRESULT GetCLRControl(out IntPtr /*  ICLRControl** */ pCLRControl);
        HRESULT UnloadAppDomain(in DWORD dwAppDomainId,
                           in bool fWaitUntilDone);
        HRESULT ExecuteInAppDomain(in DWORD dwAppDomainId,
                           in IntPtr /*FExecuteInAppDomainCallback*/ pCallback,
                           in IntPtr cookie);
        HRESULT GetCurrentAppDomainId(out DWORD* pdwAppDomainId);

        HRESULT ExecuteApplication([MarshalAs(UnmanagedType.LPWStr)] string pwzAppFullName,
                           in DWORD dwManifestPaths,
                            [MarshalAs(UnmanagedType.LPWStr)] string ppwzManifestPaths,   // optional
                           in DWORD dwActivationData,
                           [MarshalAs(UnmanagedType.LPWStr)] string ppwzActivationData,  // optional
                           out int* pReturnValue);

        HRESULT ExecuteInDefaultAppDomain([MarshalAs(UnmanagedType.LPWStr)]  string pwzAssemblyPath,
                                          [MarshalAs(UnmanagedType.LPWStr)]  string pwzTypeName,
                                          [MarshalAs(UnmanagedType.LPWStr)]  string pwzMethodName,
                                          [MarshalAs(UnmanagedType.LPWStr)] string pwzArgument,
                                          out DWORD* pReturnValue);

        HRESULT SetStartupFlags(in STARTUP_FLAGS dwFlags);
    }

    enum STARTUP_FLAGS
    {
        STARTUP_CONCURRENT_GC = 0x1,

        STARTUP_LOADER_OPTIMIZATION_MASK = 0x3 << 1,                    // loader optimization mask
        STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN = 0x1 << 1,           // no domain neutral loading
        STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN = 0x2 << 1,            // all domain neutral loading
        STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST = 0x3 << 1,       // strong name domain neutral loading

        STARTUP_LOADER_SAFEMODE = 0x10,                               // Do not apply runtime version policy to the version passed in
        STARTUP_LOADER_SETPREFERENCE = 0x100,                         // Set preferred runtime. Do not actally start it

        STARTUP_SERVER_GC = 0x1000,                       // Use server GC
        STARTUP_HOARD_GC_VM = 0x2000,                       // GC keeps virtual address used
        STARTUP_SINGLE_VERSION_HOSTING_INTERFACE = 0x4000,                    // Disallow mixing hosting interface
        STARTUP_LEGACY_IMPERSONATION = 0x10000,                        // Do not flow impersonation across async points by default
        STARTUP_DISABLE_COMMITTHREADSTACK = 0x20000,           // Don't eagerly commit thread stack
        STARTUP_ALWAYSFLOW_IMPERSONATION = 0x40000,                        // Force flow impersonation across async points 
                                                                           // (impersonations achieved thru p/invoke and managed will flow. 
                                                                           // default is to flow only managed impersonation)
        STARTUP_TRIM_GC_COMMIT = 0x80000,                      // GC uses less committed space when system memory low
        STARTUP_ETW = 0x100000,
        STARTUP_ARM = 0x400000,                     // Enable the ARM feature.
        STARTUP_SINGLE_APPDOMAIN = 0x800000,                      // application runs in default domain, no more domains are created 
        STARTUP_APPX_APP_MODEL = 0x1000000,                     // jupiter app
        STARTUP_DISABLE_RANDOMIZED_STRING_HASHING = 0x2000000      // Disable the randomized string hashing (not supported)
    }
}