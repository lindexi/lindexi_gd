// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkStrokesEvents
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [InterfaceType(2)]
  [Guid("F33053EC-5D25-430A-928F-76A6491DDE15")]
  [TypeLibType(4096)]
  [ComImport]
  internal interface _IInkStrokesEvents
  {
    [DispId(1)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void StrokesAdded([MarshalAs(UnmanagedType.Struct), In] object StrokeIds);

    [DispId(2)]
    [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void StrokesRemoved([MarshalAs(UnmanagedType.Struct), In] object StrokeIds);
  }
}
