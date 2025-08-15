using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("177160ca-bb5a-411c-841d-bd38facdeaa0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe partial interface IShellDispatch3 : IShellDispatch2
{
    [PreserveSig]
    [helpstring("Add an object to the Recent Docuements")]
    HRESULT AddToRecent(TodoArguments todo);
}