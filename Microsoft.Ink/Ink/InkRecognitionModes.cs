// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkRecognitionModes
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkRecognitionModes
  {
    IRM_None = 0,
    IRM_WordModeOnly = 1,
    IRM_Coerce = 2,
    IRM_TopInkBreaksOnly = 4,
    IRM_PrefixOk = 8,
    IRM_LineMode = 16, // 0x00000010
    IRM_DisablePersonalization = 32, // 0x00000020
    IRM_AutoSpace = 64, // 0x00000040
    [TypeLibVar(64)] IRM_Max = 128, // 0x00000080
  }
}
