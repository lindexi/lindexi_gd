using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("00020400-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
public unsafe partial interface IDispatch
{
    HRESULT GetTypeInfoCount(uint* pctinfo);

    HRESULT GetTypeInfo(TodoArguments todo);
    HRESULT GetIDsOfNames(TodoArguments todo);

    HRESULT Invoke(TodoArguments todo);
}

