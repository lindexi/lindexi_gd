// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkClipboardModes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink
{
  [Flags]
  public enum InkClipboardModes
  {
    Copy = 0,
    Cut = 1,
    ExtractOnly = 48, // 0x00000030
    DelayedCopy = 32, // 0x00000020
    Default = 0,
  }
}
