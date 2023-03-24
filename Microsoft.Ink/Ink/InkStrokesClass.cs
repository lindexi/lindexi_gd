// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkStrokesClass
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.CustomMarshalers;
using System.Security;

namespace Microsoft.Ink
{
  [ComSourceInterfaces("Microsoft.Ink._IInkStrokesEvents\0\0")]
  [ClassInterface(0)]
  [Guid("48F491BC-240E-4860-B079-A1E94D3D2C86")]
  [SuppressUnmanagedCodeSecurity]
  [DefaultMember("Item")]
  [ComImport]
  internal class InkStrokesClass : IInkStrokes, InkStrokes, _IInkStrokesEvents_Event, IEnumerable
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    internal extern InkStrokesClass();

    [DispId(1)]
    public virtual extern int Count { [DispId(1), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] get; }

    [TypeLibFunc(1)]
    [DispId(-4)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (EnumeratorToEnumVariantMarshaler))]
    public virtual extern IEnumerator GetEnumerator();

    [DispId(3)]
    public virtual extern InkDisp Ink { [DispId(3), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(18)]
    public virtual extern IInkRecognitionResult RecognitionResult { [DispId(18), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }

    [DispId(8)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.BStr)]
    public new virtual extern string ToString();

    [DispId(0)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern IInkStrokeDisp Item([In] int Index);

    [DispId(4)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Add([MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp InkStroke);

    [DispId(5)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void AddStrokes([MarshalAs(UnmanagedType.Interface), In] InkStrokes InkStrokes);

    [DispId(6)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Remove([MarshalAs(UnmanagedType.Interface), In] IInkStrokeDisp InkStroke);

    [DispId(7)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void RemoveStrokes([MarshalAs(UnmanagedType.Interface), In] InkStrokes InkStrokes);

    [DispId(9)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void ModifyDrawingAttributes([MarshalAs(UnmanagedType.Interface), In] InkDrawingAttributes DrawAttrs);

    [DispId(10)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    public virtual extern InkRectangle GetBoundingBox([In] InkBoundingBoxMode BoundingBoxMode = InkBoundingBoxMode.IBBM_Default);

    [DispId(12)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Transform([MarshalAs(UnmanagedType.Interface), In] InkTransform Transform, [In] bool ApplyOnPenWidth = false);

    [DispId(11)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void ScaleToRectangle([MarshalAs(UnmanagedType.Interface), In] InkRectangle Rectangle);

    [DispId(13)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Move([In] float HorizontalComponent, [In] float VerticalComponent);

    [DispId(14)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Rotate([In] float Degrees, [In] float x = 0.0f, [In] float y = 0.0f);

    [DispId(15)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Shear([In] float HorizontalMultiplier, [In] float VerticalMultiplier);

    [DispId(16)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void ScaleTransform([In] float HorizontalMultiplier, [In] float VerticalMultiplier);

    [DispId(17)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void Clip([MarshalAs(UnmanagedType.Interface), In] InkRectangle Rectangle);

    [DispId(19)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void RemoveRecognitionResult();

    public virtual extern event _IInkStrokesEvents_StrokesAddedEventHandler StrokesAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_StrokesAdded(_IInkStrokesEvents_StrokesAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_StrokesAdded(_IInkStrokesEvents_StrokesAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void add_StrokesRemoved(_IInkStrokesEvents_StrokesRemovedEventHandler A_1);

    public virtual extern event _IInkStrokesEvents_StrokesRemovedEventHandler StrokesRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public virtual extern void remove_StrokesRemoved(
      _IInkStrokesEvents_StrokesRemovedEventHandler A_1);
  }
}
