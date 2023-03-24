// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink._IInkOverlayEvents_CursorButtonDownEventHandler
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [ComVisible(false)]
  [TypeLibType(16)]
  internal delegate void _IInkOverlayEvents_CursorButtonDownEventHandler(
    [MarshalAs(UnmanagedType.Interface), In] IInkCursor Cursor,
    [MarshalAs(UnmanagedType.Interface), In] IInkCursorButton Button);
}
