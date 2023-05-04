using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[SuppressUnmanagedCodeSecurity]
[Guid("76BA3491-CB2F-406B-9961-0E0C4CDAAEF2")]
[DefaultMember("AddWord")]
[TypeLibType(4160)]
[ComImport]
internal interface IInkWordList
{
    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void AddWord([MarshalAs(UnmanagedType.BStr), In] string NewWord);

    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemoveWord([MarshalAs(UnmanagedType.BStr), In] string RemoveWord);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Merge([MarshalAs(UnmanagedType.Interface), In] InkWordList MergeWordList);
}