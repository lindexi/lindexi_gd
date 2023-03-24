// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IPenInputPanelEvents_Event
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComEventInterface(typeof (_IPenInputPanelEvents), typeof (_IPenInputPanelEvents_EventProvider))]
  [ComVisible(false)]
  [TypeLibType(16)]
  internal interface _IPenInputPanelEvents_Event
  {
    event _IPenInputPanelEvents_InputFailedEventHandler InputFailed;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_InputFailed(_IPenInputPanelEvents_InputFailedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_InputFailed(_IPenInputPanelEvents_InputFailedEventHandler A_1);

    event _IPenInputPanelEvents_VisibleChangedEventHandler VisibleChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_VisibleChanged(
      _IPenInputPanelEvents_VisibleChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_VisibleChanged(
      _IPenInputPanelEvents_VisibleChangedEventHandler A_1);

    event _IPenInputPanelEvents_PanelChangedEventHandler PanelChanged;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_PanelChanged(_IPenInputPanelEvents_PanelChangedEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_PanelChanged(_IPenInputPanelEvents_PanelChangedEventHandler A_1);

    event _IPenInputPanelEvents_PanelMovingEventHandler PanelMoving;

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void add_PanelMoving(_IPenInputPanelEvents_PanelMovingEventHandler A_1);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void remove_PanelMoving(_IPenInputPanelEvents_PanelMovingEventHandler A_1);
  }
}
