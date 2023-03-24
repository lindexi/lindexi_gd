// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkRecognitionStatusPrivate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkRecognitionStatusPrivate
  {
    IRS_NoError = 0,
    IRS_Interrupted = 1,
    IRS_ProcessFailed = 2,
    IRS_InkAddedFailed = 4,
    IRS_SetAutoCompletionModeFailed = 8,
    IRS_SetStrokesFailed = 16, // 0x00000010
    IRS_SetGuideFailed = 32, // 0x00000020
    IRS_SetFlagsFailed = 64, // 0x00000040
    IRS_SetFactoidFailed = 128, // 0x00000080
    IRS_SetPrefixSuffixFailed = 256, // 0x00000100
    IRS_SetWordListFailed = 512, // 0x00000200
  }
}
