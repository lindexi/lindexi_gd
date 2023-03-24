// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkOverlayPrivate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;

namespace Microsoft.Ink
{
  [Guid("B82A463B-C1C5-45A3-997C-DEAB5651B67A")]
  [CoClass(typeof (InkOverlayClass))]
  [ComImport]
  internal interface InkOverlayPrivate : IInkOverlay, _IInkOverlayEvents_Event
  {
  }
}
