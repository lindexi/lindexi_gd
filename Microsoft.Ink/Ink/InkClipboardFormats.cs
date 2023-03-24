// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkClipboardFormats
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink
{
  [Flags]
  public enum InkClipboardFormats
  {
    None = 0,
    InkSerializedFormat = 1,
    SketchInk = 2,
    TextInk = 6,
    EnhancedMetafile = 8,
    Metafile = 32, // 0x00000020
    Bitmap = 64, // 0x00000040
    PasteMask = TextInk | InkSerializedFormat, // 0x00000007
    CopyMask = 127, // 0x0000007F
    Default = CopyMask, // 0x0000007F
  }
}
