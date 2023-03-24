// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkDispClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [Guid("937C1A34-151D-4610-9CA6-A8CC9BDB5D83")]
  [SuppressUnmanagedCodeSecurity]
  [ComSourceInterfaces("Microsoft.Ink._IInkEvents\0\0")]
  [TypeLibType(2)]
  [ClassInterface(0)]
  [ComImport]
  internal class InkDispClass : IInkDisp, InkDisp, _IInkEvents_Event
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern InkDispClass();

    [DispId(1)]
    public virtual extern InkStrokes Strokes { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(2)]
    public virtual extern IInkExtendedProperties ExtendedProperties { [DispId(2), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(8)]
    public virtual extern bool Dirty { [DispId(8), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; [DispId(8), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [param: In] set; }

    [DispId(9)]
    public virtual extern IInkCustomStrokes CustomStrokes { [DispId(9), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(3)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkRectangle GetBoundingBox([In] InkBoundingBoxMode BoundingBoxMode = InkBoundingBoxMode.IBBM_Default);

    [DispId(4)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void DeleteStrokes([MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes = null);

    [DispId(5)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void DeleteStroke([MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp Stroke);

    [DispId(6)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkDisp ExtractStrokes([MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes = null, [In] InkExtractFlags ExtractFlags = InkExtractFlags.IEF_RemoveFromOriginal);

    [DispId(7)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkDisp ExtractWithRectangle(
      [MarshalAs(UnmanagedType.Interface), In] InkRectangle Rectangle,
      [In] InkExtractFlags ExtractFlags = InkExtractFlags.IEF_RemoveFromOriginal);

    [DispId(18)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Clip([MarshalAs(UnmanagedType.Interface), In] InkRectangle Rectangle);

    [DispId(10)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkDisp Clone();

    [DispId(11)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkStrokes HitTestCircle([In] int x, [In] int y, [In] float radius);

    [DispId(12)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkStrokes HitTestWithRectangle(
      [MarshalAs(UnmanagedType.Interface), In] InkRectangle SelectionRectangle,
      [In] float IntersectPercent);

    [DispId(13)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkStrokes HitTestWithLasso(
      [MarshalAs(UnmanagedType.Struct), In] object Points,
      [In] float IntersectPercent,
      [MarshalAs(UnmanagedType.Struct), In, Out] ref object LassoPoints = 0);

    [DispId(14)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern IInkStrokeDisp NearestPoint(
      [In] int x,
      [In] int y,
      [In, Out] ref float PointOnStroke = 0.0f,
      [In, Out] ref float DistanceFromPacket = 0.0f);

    [DispId(15)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkStrokes CreateStrokes([MarshalAs(UnmanagedType.Struct), In] object StrokeIds = 0);

    [DispId(17)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void AddStrokesAtRectangle(
      [MarshalAs(UnmanagedType.Interface), In] InkStrokes SourceStrokes,
      [MarshalAs(UnmanagedType.Interface), In] InkRectangle TargetRectangle);

    [DispId(19)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Struct)]
    public virtual extern object Save(
      [In] InkPersistenceFormat PersistenceFormat = InkPersistenceFormat.IPF_InkSerializedFormat,
      [In] InkPersistenceCompressionMode CompressionMode = InkPersistenceCompressionMode.IPCM_Default);

    [DispId(20)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Load([MarshalAs(UnmanagedType.Struct), In] object Data);

    [DispId(16)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern IInkStrokeDisp CreateStroke([MarshalAs(UnmanagedType.Struct), In] object PacketData, [MarshalAs(UnmanagedType.Struct), In] object PacketDescription);

    [DispId(22)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern IDataObject ClipboardCopyWithRectangle(
      [MarshalAs(UnmanagedType.Interface), In] InkRectangle Rectangle,
      [In] InkClipboardFormatsPrivate ClipboardFormats = InkClipboardFormatsPrivate.ICF_CopyMask,
      [In] InkClipboardModesPrivate ClipboardModes = InkClipboardModesPrivate.ICB_Copy);

    [DispId(23)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern IDataObject ClipboardCopy(
      [MarshalAs(UnmanagedType.Interface), In] InkStrokes Strokes = null,
      [In] InkClipboardFormatsPrivate ClipboardFormats = InkClipboardFormatsPrivate.ICF_CopyMask,
      [In] InkClipboardModesPrivate ClipboardModes = InkClipboardModesPrivate.ICB_Copy);

    [DispId(24)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern bool CanPaste([In] IntPtr DataObject = default (IntPtr));

    [DispId(25)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkStrokes ClipboardPaste([In] int x = 0, [In] int y = 0, [In] IntPtr DataObject = default (IntPtr));

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_InkAdded(_IInkEvents_InkAddedEventHandler A_1);

    public virtual extern event _IInkEvents_InkAddedEventHandler InkAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_InkAdded(_IInkEvents_InkAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_InkDeleted(_IInkEvents_InkDeletedEventHandler A_1);

    public virtual extern event _IInkEvents_InkDeletedEventHandler InkDeleted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_InkDeleted(_IInkEvents_InkDeletedEventHandler A_1);
  }
}
