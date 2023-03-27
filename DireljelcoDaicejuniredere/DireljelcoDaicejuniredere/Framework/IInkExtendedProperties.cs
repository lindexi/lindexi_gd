using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[Guid("89F2A8BE-95A9-4530-8B8F-88E971E3E25F")]
[TypeLibType(4160)]
[SuppressUnmanagedCodeSecurity]
[DefaultMember("Item")]
[ComImport]
internal interface IInkExtendedProperties : IEnumerable
{
    [DispId(1)]
    int Count { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [TypeLibFunc(1)]
    [DispId(-4)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler")]
    new IEnumerator GetEnumerator();

    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    IInkExtendedProperty Item([MarshalAs(UnmanagedType.Struct), In] object Identifier);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    IInkExtendedProperty Add([MarshalAs(UnmanagedType.BStr), In] string Guid, [MarshalAs(UnmanagedType.Struct), In] object Data);

    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Remove([MarshalAs(UnmanagedType.Struct), In] object Identifier);

    [DispId(4)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Clear();

    [DispId(5)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    bool DoesPropertyExist([MarshalAs(UnmanagedType.BStr), In] string Guid);
}