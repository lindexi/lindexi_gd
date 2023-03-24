// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.TextInputPanelClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [Guid("F9B189D7-228B-4F2B-8650-B97F59E02C8C")]
  [ClassInterface(0)]
  [TypeLibType(2)]
  [ComImport]
  internal class TextInputPanelClass : 
    ITextInputPanel,
    TextInputPanel,
    IInputPanelWindowHandle,
    ITextInputPanelRunInfo
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern TextInputPanelClass();

    [DispId(1610678272)]
    [ComAliasName("Microsoft.Ink.wireHWND")]
    public virtual extern IntPtr AttachedEditWindow { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.wireHWND")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.wireHWND"), In] set; }

    [ComAliasName("Microsoft.Ink.InteractionMode")]
    [DispId(1610678274)]
    public virtual extern InteractionMode CurrentInteractionMode { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InteractionMode")] get; }

    [DispId(1610678275)]
    [ComAliasName("Microsoft.Ink.InPlaceState")]
    public virtual extern InPlaceState DefaultInPlaceState { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InPlaceState")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.InPlaceState"), In] set; }

    [DispId(1610678277)]
    [ComAliasName("Microsoft.Ink.InPlaceState")]
    public virtual extern InPlaceState CurrentInPlaceState { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InPlaceState")] get; }

    [DispId(1610678278)]
    [ComAliasName("Microsoft.Ink.PanelInputArea")]
    public virtual extern PanelInputArea DefaultInputArea { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.PanelInputArea")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.PanelInputArea"), In] set; }

    [DispId(1610678280)]
    [ComAliasName("Microsoft.Ink.PanelInputArea")]
    public virtual extern PanelInputArea CurrentInputArea { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.PanelInputArea")] get; }

    [ComAliasName("Microsoft.Ink.CorrectionMode")]
    [DispId(1610678281)]
    public virtual extern CorrectionMode CurrentCorrectionMode { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.CorrectionMode")] get; }

    [DispId(1610678282)]
    [ComAliasName("Microsoft.Ink.InPlaceDirection")]
    public virtual extern InPlaceDirection PreferredInPlaceDirection { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: ComAliasName("Microsoft.Ink.InPlaceDirection")] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: ComAliasName("Microsoft.Ink.InPlaceDirection"), In] set; }

    [DispId(1610678284)]
    public virtual extern int ExpandPostInsertionCorrection { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(1610678286)]
    public virtual extern int InPlaceVisibleOnFocus { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(1610678288)]
    public virtual extern tagRECT InPlaceBoundingRectangle { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(1610678289)]
    public virtual extern int PopUpCorrectionHeight { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [DispId(1610678290)]
    public virtual extern int PopDownCorrectionHeight { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void CommitPendingInput();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetInPlaceVisibility(int Visible);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetInPlacePosition(
      int xPosition,
      int yPosition,
      [ComAliasName("Microsoft.Ink.CorrectionPosition")] CorrectionPosition position);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void SetInPlaceHoverTargetPosition(int xPosition, int yPosition);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Advise([MarshalAs(UnmanagedType.Interface)] ITextInputPanelEventSink EventSink, uint EventMask);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Unadvise([MarshalAs(UnmanagedType.Interface)] ITextInputPanelEventSink EventSink);

    public virtual extern int AttachedEditWindow32 { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    public virtual extern long AttachedEditWindow64 { [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void IsTipRunning(out int pfRunning);
  }
}
