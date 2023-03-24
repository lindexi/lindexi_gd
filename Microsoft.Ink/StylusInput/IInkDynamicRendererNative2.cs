// Decompiled with JetBrains decompiler
// Type: Microsoft.StylusInput.IInkDynamicRendererNative2
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.StylusInput
{
  [Guid("2E800077-40A1-44fd-9485-2B14D83669C3")]
  [SuppressUnmanagedCodeSecurity]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IInkDynamicRendererNative2
  {
    void Draw(IntPtr hDC);

    IntPtr get_ClipRegion();

    void set_ClipRegion(IntPtr hClipRgn);
  }
}
