using System.Diagnostics;
using System.Runtime.InteropServices;

unsafe
{
    CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED | COINIT.COINIT_DISABLE_OLE1DDE);

    var rclsid = Guid.Parse("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
    var riid = Guid.Parse("d57c7288-d4ad-4768-be02-9d969532d960");

    void* ppv;

    var hr = CoCreateInstance(&rclsid, IntPtr.Zero,
        CLSCTX.CLSCTX_ALL, &riid, &ppv);
    Debug.Assert(hr == 0);

    void*** pvtable = (void***) ppv;
    // void Show(IntPtr owner);
    var owner = IntPtr.Zero;
    ((delegate* unmanaged[MemberFunction]<void*, nint, int>) (*pvtable)[3])(ppv, owner);
}

[DllImport("OLE32.dll", ExactSpelling = true, EntryPoint = "CoInitializeEx")]
static extern unsafe int CoInitializeEx([Optional] void* pvReserved, [MarshalAs(UnmanagedType.U4)] COINIT dwCoInit);

[DllImport("OLE32.dll", ExactSpelling = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
static extern unsafe int CoCreateInstance(Guid* rclsid, IntPtr pUnkOuter, CLSCTX dwClsContext, Guid* riid, void* ppv);

enum CLSCTX : uint
{
    /// <summary>The code that creates and manages objects of this class is a DLL that runs in the same process as the caller of the function specifying the class context.</summary>
    CLSCTX_INPROC_SERVER = 0x00000001,

    /// <summary>The code that manages objects of this class is an in-process handler. This is a DLL that runs in the client process and implements client-side structures of this class when instances of the class are accessed remotely.</summary>
    CLSCTX_INPROC_HANDLER = 0x00000002,

    /// <summary>The EXE code that creates and manages objects of this class runs on same machine but is loaded in a separate process space.</summary>
    CLSCTX_LOCAL_SERVER = 0x00000004,

    /// <summary>Obsolete.</summary>
    CLSCTX_INPROC_SERVER16 = 0x00000008,

    /// <summary>A remote context. The <a href="https://docs.microsoft.com/windows/desktop/com/localserver32">LocalServer32</a> or <a href="https://docs.microsoft.com/windows/desktop/com/localservice">LocalService</a> code that creates and manages objects of this class is run on a different computer.</summary>
    CLSCTX_REMOTE_SERVER = 0x00000010,

    /// <summary>Obsolete.</summary>
    CLSCTX_INPROC_HANDLER16 = 0x00000020,

    /// <summary>Reserved.</summary>
    CLSCTX_RESERVED1 = 0x00000040,

    /// <summary>Reserved.</summary>
    CLSCTX_RESERVED2 = 0x00000080,

    /// <summary>Reserved.</summary>
    CLSCTX_RESERVED3 = 0x00000100,

    /// <summary>Reserved.</summary>
    CLSCTX_RESERVED4 = 0x00000200,

    /// <summary>Disables the downloading of code from the directory service or the Internet. This flag cannot be set at the same time as CLSCTX_ENABLE_CODE_DOWNLOAD.</summary>
    CLSCTX_NO_CODE_DOWNLOAD = 0x00000400,

    /// <summary>Reserved.</summary>
    CLSCTX_RESERVED5 = 0x00000800,

    /// <summary>Specify if you want the activation to fail if it uses custom marshalling.</summary>
    CLSCTX_NO_CUSTOM_MARSHAL = 0x00001000,

    /// <summary>Enables the downloading of code from the directory service or the Internet. This flag cannot be set at the same time as CLSCTX_NO_CODE_DOWNLOAD.</summary>
    CLSCTX_ENABLE_CODE_DOWNLOAD = 0x00002000,

    /// <summary>
    /// <para>The CLSCTX_NO_FAILURE_LOG can be used to override the logging of failures in <a href="https://docs.microsoft.com/windows/desktop/api/combaseapi/nf-combaseapi-cocreateinstanceex">CoCreateInstanceEx</a>. If the ActivationFailureLoggingLevel is created, the following values can determine the status of event logging: </para>
    /// <para>This doc was truncated.</para>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    CLSCTX_NO_FAILURE_LOG = 0x00004000,

    /// <summary>
    /// <para>Disables activate-as-activator (AAA) activations for this activation only. This flag overrides the setting of the EOAC_DISABLE_AAA flag from the EOLE_AUTHENTICATION_CAPABILITIES enumeration. This flag cannot be set at the same time as CLSCTX_ENABLE_AAA. Any activation where a server process would be launched under the caller's identity is known as an activate-as-activator (AAA) activation. Disabling AAA activations allows an application that runs under a privileged account (such as LocalSystem) to help prevent its identity from being used to launch untrusted components. Library applications that use activation calls should always set this flag during those calls. This helps prevent the library application from being used in an escalation-of-privilege security attack. This is the only way to disable AAA activations in a library application because the EOAC_DISABLE_AAA flag from the EOLE_AUTHENTICATION_CAPABILITIES enumeration is applied only to the server process and not to the library application. <b>Windows 2000:  </b>This flag is not supported.</para>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    CLSCTX_DISABLE_AAA = 0x00008000,

    /// <summary>
    /// <para>Enables activate-as-activator (AAA) activations for this activation only. This flag overrides the setting of the EOAC_DISABLE_AAA flag from the EOLE_AUTHENTICATION_CAPABILITIES enumeration. This flag cannot be set at the same time as CLSCTX_DISABLE_AAA. Any activation where a server process would be launched under the caller's identity is known as an activate-as-activator (AAA) activation. Enabling this flag allows an application to transfer its identity to an activated component. <b>Windows 2000:  </b>This flag is not supported.</para>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    CLSCTX_ENABLE_AAA = 0x00010000,

    /// <summary>Begin this activation from the default context of the current apartment.</summary>
    CLSCTX_FROM_DEFAULT_CONTEXT = 0x00020000,

    /// <summary></summary>
    CLSCTX_ACTIVATE_X86_SERVER = 0x00040000,

    /// <summary>Activate or connect to a 32-bit version of the server; fail if one is not registered.</summary>
    CLSCTX_ACTIVATE_32_BIT_SERVER = 0x00040000,

    /// <summary>Activate or connect to a 64 bit version of the server; fail if one is not registered.</summary>
    CLSCTX_ACTIVATE_64_BIT_SERVER = 0x00080000,

    /// <summary>
    /// <para>When this flag is specified, COM uses the impersonation token of the thread, if one is present, for the activation request made by the thread. When this flag is not specified or if the thread does not have an impersonation token, COM uses the process token of the thread's process for the activation request made by the thread.</para>
    /// <para><b>Windows Vista or later:  </b>This flag is supported.</para>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    CLSCTX_ENABLE_CLOAKING = 0x00100000,

    /// <summary>
    /// <para>Indicates activation is for an app container.</para>
    /// <para><div class="alert"><b>Note</b>  This flag is reserved for internal use and is not intended to be used directly from your code.</div> <div> </div></para>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    CLSCTX_APPCONTAINER = 0x00400000,

    /// <summary>
    /// <para>Specify this flag for Interactive User activation behavior for As-Activator servers. A strongly named Medium IL Windows Store app can use this flag to launch an "As Activator" COM server without a strong name. Also, you can use this flag to bind to a running instance of the COM server that's launched by a desktop application. The client must be Medium IL, it must be strongly named, which means that it has a SysAppID in the client token, it can't be in session 0,  and it must have the same user as the session ID's user in the client token. If  the server is out-of-process and "As Activator", it launches the server with the token of the client token's session user. This token won't be strongly named. If the server is out-of-process and RunAs "Interactive User", this flag has no effect. If the server is out-of-process and is any other RunAs type, the activation fails. This flag has no effect for in-process servers. Off-machine activations fail when they use this flag.</para>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    CLSCTX_ACTIVATE_AAA_AS_IU = 0x00800000,

    /// <summary></summary>
    CLSCTX_RESERVED6 = 0x01000000,

    /// <summary></summary>
    CLSCTX_ACTIVATE_ARM32_SERVER = 0x02000000,
    CLSCTX_ALLOW_LOWER_TRUST_REGISTRATION = 0x04000000,

    /// <summary>
    /// <para>Used for loading Proxy/Stub DLLs.</para>
    /// <para><div class="alert"><b>Note</b>  This flag is reserved for internal use and is not intended to be used directly from your code.</div> <div> </div></para>
    /// <para><see href="https://learn.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    CLSCTX_PS_DLL = 0x80000000,
    CLSCTX_ALL = 0x00000017,
    CLSCTX_SERVER = 0x00000015,
}

enum COINIT
{
    /// <summary>Initializes the thread for apartment-threaded object concurrency (see Remarks).</summary>
    COINIT_APARTMENTTHREADED = 0x00000002,

    /// <summary>Initializes the thread for multithreaded object concurrency (see Remarks).</summary>
    COINIT_MULTITHREADED = 0x00000000,

    /// <summary>Disables DDE for OLE1 support.</summary>
    COINIT_DISABLE_OLE1DDE = 0x00000004,

    /// <summary>Increase memory usage in an attempt to increase performance.</summary>
    COINIT_SPEED_OVER_MEMORY = 0x00000008,
}