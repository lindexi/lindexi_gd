// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.ITextInputPanelEventSink
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [Guid("27560408-8E64-4FE1-804E-421201584B31")]
  [TypeLibType(256)]
  [SuppressUnmanagedCodeSecurity]
  [InterfaceType(1)]
  [ComImport]
  internal interface ITextInputPanelEventSink
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InPlaceStateChanging([ComAliasName("Microsoft.Ink.InPlaceState"), In] InPlaceState oldInPlaceState, [ComAliasName("Microsoft.Ink.InPlaceState"), In] InPlaceState newInPlaceState);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InPlaceStateChanged([ComAliasName("Microsoft.Ink.InPlaceState"), In] InPlaceState oldInPlaceState, [ComAliasName("Microsoft.Ink.InPlaceState"), In] InPlaceState newInPlaceState);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InPlaceSizeChanging([In] tagRECT oldBoundingRectangle, [In] tagRECT newBoundingRectangle);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InPlaceSizeChanged([In] tagRECT oldBoundingRectangle, [In] tagRECT newBoundingRectangle);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InputAreaChanging([ComAliasName("Microsoft.Ink.PanelInputArea"), In] PanelInputArea oldInputArea, [ComAliasName("Microsoft.Ink.PanelInputArea"), In] PanelInputArea newInputArea);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InputAreaChanged([ComAliasName("Microsoft.Ink.PanelInputArea"), In] PanelInputArea oldInputArea, [ComAliasName("Microsoft.Ink.PanelInputArea"), In] PanelInputArea newInputArea);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CorrectionModeChanging([ComAliasName("Microsoft.Ink.CorrectionMode"), In] CorrectionMode oldCorrectionMode, [ComAliasName("Microsoft.Ink.CorrectionMode"), In] CorrectionMode newCorrectionMode);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CorrectionModeChanged([ComAliasName("Microsoft.Ink.CorrectionMode"), In] CorrectionMode oldCorrectionMode, [ComAliasName("Microsoft.Ink.CorrectionMode"), In] CorrectionMode newCorrectionMode);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InPlaceVisibilityChanging([In] int oldVisible, [In] int newVisible);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void InPlaceVisibilityChanged([In] int oldVisible, [In] int newVisible);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void TextInserting([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH), In] Array InkPrivate);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void TextInserted([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH), In] Array InkPrivate);
  }
}
