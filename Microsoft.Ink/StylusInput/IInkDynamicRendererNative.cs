// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.IInkDynamicRendererNative
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using Microsoft.Ink;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.StylusInput
{
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  [SuppressUnmanagedCodeSecurity]
  [Guid("2F59E338-88F1-4ad5-A46F-B52C706A8393")]
  internal interface IInkDynamicRendererNative
  {
    IntPtr get_hWnd();

    void set_hWnd(IntPtr hdc);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_Enabled();

    void set_Enabled([MarshalAs(UnmanagedType.Bool), In] bool enable);

    [return: MarshalAs(UnmanagedType.Interface)]
    IInkDrawingAttributes get_DrawingAttributes();

    void set_DrawingAttributes(IInkDrawingAttributes attrs);

    [return: MarshalAs(UnmanagedType.Struct)]
    tagRECT get_ClipRectangle();

    void set_ClipRectangle([MarshalAs(UnmanagedType.Struct), In] ref tagRECT clipRectangle);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_EnableDataCache();

    void set_EnableDataCache([MarshalAs(UnmanagedType.Bool), In] bool cache);

    void ReleaseCachedData(uint cachedDataId);

    void Refresh();
  }
}
