using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace WhefallralajaHubeanerelair;

[DefaultMember("AddWord")]
[Guid("9DE85094-F71F-44F1-8471-15A2FA76FCF3")]
[ClassInterface((short) 0)]
[TypeLibType(2)]
[SuppressUnmanagedCodeSecurity]
[ComImport]
internal class InkWordListClass : IInkWordList, InkWordList
{
    //[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    //public extern InkWordListClass();

    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void AddWord([MarshalAs(UnmanagedType.BStr), In] string newWord);

    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void RemoveWord([MarshalAs(UnmanagedType.BStr), In] string removeWord);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Merge([MarshalAs(UnmanagedType.Interface), In] InkWordList mergeWordList);
}