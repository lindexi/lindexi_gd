using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[Guid("14542586-11BF-4F5F-B6E7-49D0744AAB6E")]
[SuppressUnmanagedCodeSecurity]
[TypeLibType(4160)]
[ComImport]
internal interface IInkWordList2
{
    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void AddWords([MarshalAs(UnmanagedType.BStr), In] string NewWords);
}