// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkEvents_Event
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComVisible(false)]
  [ComEventInterface(typeof (_IInkEvents), typeof (_IInkEvents_EventProvider))]
  [TypeLibType(16)]
  internal interface _IInkEvents_Event
  {
    event _IInkEvents_InkAddedEventHandler InkAdded;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_InkAdded(_IInkEvents_InkAddedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_InkAdded(_IInkEvents_InkAddedEventHandler A_1);

    event _IInkEvents_InkDeletedEventHandler InkDeleted;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_InkDeleted(_IInkEvents_InkDeletedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_InkDeleted(_IInkEvents_InkDeletedEventHandler A_1);
  }
}
