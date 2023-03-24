// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognitionModes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink
{
  [Flags]
  public enum RecognitionModes
  {
    None = 0,
    WordMode = 1,
    TopInkBreaksOnly = 4,
    Coerce = 2,
    PrefixOk = 8,
    LineMode = 16, // 0x00000010
    DisablePersonalization = 32, // 0x00000020
    AutoSpace = 64, // 0x00000040
  }
}
