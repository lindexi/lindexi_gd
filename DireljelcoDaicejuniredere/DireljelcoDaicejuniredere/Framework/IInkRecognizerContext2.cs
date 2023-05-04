using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[SuppressUnmanagedCodeSecurity]
[Guid("D6F0E32F-73D8-408E-8E9F-5FEA592C363F")]
[TypeLibType(4160)]
[DefaultMember("EnabledUnicodeRanges")]
[ComImport]
internal interface IInkRecognizerContext2
{
    [DispId(0)]
    object EnabledUnicodeRanges { [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][return: MarshalAs(UnmanagedType.Struct)] get; [DispId(0), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)][param: MarshalAs(UnmanagedType.Struct), In] set; }
}