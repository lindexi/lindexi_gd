// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkEvents
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [InterfaceType(2)]
  [Guid("427B1865-CA3F-479A-83A9-0F420F2A0073")]
  [TypeLibType(4096)]
  [ComImport]
  internal interface _IInkEvents
  {
    [DispId(1)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InkAdded([MarshalAs(UnmanagedType.Struct), In] object StrokeIds);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InkDeleted([MarshalAs(UnmanagedType.Struct), In] object StrokeIds);
  }
}
