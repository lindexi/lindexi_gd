// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RecognitionStatus
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink
{
  [Flags]
  public enum RecognitionStatus
  {
    NoError = 0,
    Interrupted = 1,
    ProcessFailed = 2,
    InkAddedFailed = 4,
    SetAutoCompletionModeFailed = 8,
    SetStrokesFailed = 16, // 0x00000010
    SetGuideFailed = 32, // 0x00000020
    SetFlagsFailed = 64, // 0x00000040
    SetFactoidFailed = 128, // 0x00000080
    SetPrefixSuffixFailed = 256, // 0x00000100
    SetWordListFailed = 512, // 0x00000200
  }
}
