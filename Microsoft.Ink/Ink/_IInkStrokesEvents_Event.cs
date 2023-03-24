// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkStrokesEvents_Event
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComVisible(false)]
  [TypeLibType(16)]
  [ComEventInterface(typeof (_IInkStrokesEvents), typeof (_IInkStrokesEvents_EventProvider))]
  internal interface _IInkStrokesEvents_Event
  {
    event _IInkStrokesEvents_StrokesAddedEventHandler StrokesAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_StrokesAdded(_IInkStrokesEvents_StrokesAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_StrokesAdded(_IInkStrokesEvents_StrokesAddedEventHandler A_1);

    event _IInkStrokesEvents_StrokesRemovedEventHandler StrokesRemoved;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_StrokesRemoved(_IInkStrokesEvents_StrokesRemovedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_StrokesRemoved(_IInkStrokesEvents_StrokesRemovedEventHandler A_1);
  }
}
