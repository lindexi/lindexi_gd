using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("efd84b2d-4bcf-4298-be25-eb542a59fbda")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe partial interface IShellDispatch4 : IShellDispatch3
{
    [PreserveSig]
    [helpstring("Windows Security")]
    HRESULT WindowsSecurity();

    [PreserveSig]
    [helpstring("Raise/lower the desktop")]
    HRESULT ToggleDesktop();

    [PreserveSig]
    [helpstring("Return explorer policy value")]
    HRESULT ExplorerPolicy(TodoArguments todo);

    [PreserveSig]
    [helpstring("Return shell global setting")]
    HRESULT GetSetting(TodoArguments todo);
}