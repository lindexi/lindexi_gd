using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace DireljelcoDaicejuniredere;

[Guid("A248C1AC-C698-4E06-9E5C-D57F77C7E647")]
[SuppressUnmanagedCodeSecurity]
[DefaultMember("Item")]
[TypeLibType(4160)]
[ComImport]
internal interface IInkCursors : IEnumerable
{
    [DispId(1)]
    int Count { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(-4)]
    [TypeLibFunc(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler,
        MarshalType = "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler")]
    new IEnumerator GetEnumerator();

    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    IInkCursor Item([In] int Index);
}