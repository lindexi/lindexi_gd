// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognizerCapabilities
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink
{
  [Flags]
  public enum RecognizerCapabilities
  {
    DontCare = 1,
    Object = 2,
    FreeInput = 4,
    LinedInput = 8,
    BoxedInput = 16, // 0x00000010
    CharacterAutoCompletionInput = 32, // 0x00000020
    RightAndDown = 64, // 0x00000040
    LeftAndDown = 128, // 0x00000080
    DownAndLeft = 256, // 0x00000100
    DownAndRight = 512, // 0x00000200
    ArbitraryAngle = 1024, // 0x00000400
    Lattice = 2048, // 0x00000800
    AdviseInkChange = 4096, // 0x00001000
    StrokeReorder = 8192, // 0x00002000
    Personalizable = 16384, // 0x00004000
    PrefersArbitraryAngle = 32768, // 0x00008000
    PrefersParagraphBreaking = 65536, // 0x00010000
    PrefersSegmentation = 131072, // 0x00020000
    Cursive = 262144, // 0x00040000
    TextPrediction = 524288, // 0x00080000
    Alpha = 1048576, // 0x00100000
    Beta = 2097152, // 0x00200000
  }
}
