using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("A4C6892C-3BA9-11d2-9DEA-00C04FB16162")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe partial interface IShellDispatch2 : IShellDispatch
{
    [PreserveSig]
    [helpstring("get restriction settings")]
    HRESULT IsRestricted(TodoArguments todo);

    [PreserveSig]
    [helpstring("Execute generic command")]
    HRESULT ShellExecute(TodoArguments todo);

    // search for a printer
    [PreserveSig]
    [helpstring("Find a Printer in the Directory Service")]
    HRESULT FindPrinter(TodoArguments todo);

    [PreserveSig]
    [helpstring("Retrieve info about the user's system")]
    HRESULT GetSystemInformation(TodoArguments todo);

    [PreserveSig]
    [helpstring("Start a service by name, and optionally set it to autostart.")]
    HRESULT ServiceStart(TodoArguments todo);

    [PreserveSig]
    [helpstring("Stop a service by name, and optionally disable autostart.")]
    HRESULT ServiceStop(TodoArguments todo);

    [PreserveSig]
    [helpstring("Determine if a service is running by name.")]
    HRESULT IsServiceRunning(TodoArguments todo);

    [PreserveSig]
    [helpstring("Determine if the current user can start/stop the named service.")]
    HRESULT CanStartStopService(TodoArguments todo);

    [PreserveSig]
    [helpstring("Show/Hide browser bar.")]
    HRESULT ShowBrowserBar(TodoArguments todo);
}