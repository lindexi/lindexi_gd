// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IPenInputPanelEvents
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [DefaultMember("VisibleChanged")]
  [InterfaceType(2)]
  [Guid("B7E489DA-3719-439F-848F-E7ACBD820F17")]
  [TypeLibType(4096)]
  [ComImport]
  internal interface _IPenInputPanelEvents
  {
    [DispId(2)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InputFailed([In] int hWnd, [In] int Key, [MarshalAs(UnmanagedType.BStr), In] string Text, [In] short ShiftKey);

    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void VisibleChanged([In] bool NewVisibility);

    [DispId(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void PanelChanged([In] PanelTypePrivate NewPanelType);

    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void PanelMoving([In, Out] ref int Left, [In, Out] ref int Top);
  }
}
