using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[TypeLibType(4160)]
[Guid("6110118A-3A75-4AD6-B2AA-04B2B72BBE65")]
[DefaultMember("Id")]
[SuppressUnmanagedCodeSecurity]
[ComImport]
internal interface IInkRecognizer2
{
    [DispId(0)]
    string Id { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.BStr)] get; }

    [DispId(1)]
    object UnicodeRanges { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Struct)] get; }
}