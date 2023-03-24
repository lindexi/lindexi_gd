// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.ITextInputPanel
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [InterfaceType(1)]
  [Guid("6B6A65A5-6AF3-46C2-B6EA-56CD1F80DF71")]
  [SuppressUnmanagedCodeSecurity]
  [ComConversionLoss]
  [TypeLibType(256)]
  [ComImport]
  internal interface ITextInputPanel
  {
    [ComAliasName("Microsoft.Ink.wireHWND")]
    [DispId(1610678272)]
    IntPtr AttachedEditWindow { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.wireHWND")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.wireHWND"), In] set; }

    [DispId(1610678274)]
    [ComAliasName("Microsoft.Ink.InteractionMode")]
    InteractionMode CurrentInteractionMode { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InteractionMode")] get; }

    [DispId(1610678275)]
    [ComAliasName("Microsoft.Ink.InPlaceState")]
    InPlaceState DefaultInPlaceState { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InPlaceState")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.InPlaceState"), In] set; }

    [DispId(1610678277)]
    [ComAliasName("Microsoft.Ink.InPlaceState")]
    InPlaceState CurrentInPlaceState { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InPlaceState")] get; }

    [DispId(1610678278)]
    [ComAliasName("Microsoft.Ink.PanelInputArea")]
    PanelInputArea DefaultInputArea { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.PanelInputArea")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.PanelInputArea"), In] set; }

    [ComAliasName("Microsoft.Ink.PanelInputArea")]
    [DispId(1610678280)]
    PanelInputArea CurrentInputArea { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.PanelInputArea")] get; }

    [DispId(1610678281)]
    [ComAliasName("Microsoft.Ink.CorrectionMode")]
    CorrectionMode CurrentCorrectionMode { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.CorrectionMode")] get; }

    [DispId(1610678282)]
    [ComAliasName("Microsoft.Ink.InPlaceDirection")]
    InPlaceDirection PreferredInPlaceDirection { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InPlaceDirection")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.InPlaceDirection"), In] set; }

    [DispId(1610678284)]
    int ExpandPostInsertionCorrection { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(1610678286)]
    int InPlaceVisibleOnFocus { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(1610678288)]
    tagRECT InPlaceBoundingRectangle { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(1610678289)]
    int PopUpCorrectionHeight { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(1610678290)]
    int PopDownCorrectionHeight { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void CommitPendingInput();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetInPlaceVisibility(int Visible);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetInPlacePosition(int xPosition, int yPosition, [ComAliasName("Microsoft.Ink.CorrectionPosition")] CorrectionPosition position);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetInPlaceHoverTargetPosition(int xPosition, int yPosition);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Advise([MarshalAs(UnmanagedType.Interface)] ITextInputPanelEventSink EventSink, uint EventMask);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Unadvise([MarshalAs(UnmanagedType.Interface)] ITextInputPanelEventSink EventSink);
  }
}
