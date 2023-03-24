// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkClipboardModesPrivate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkClipboardModesPrivate
  {
    ICB_Copy = 0,
    ICB_Default = 0,
    ICB_Cut = 1,
    ICB_DelayedCopy = 32, // 0x00000020
    ICB_ExtractOnly = 48, // 0x00000030
  }
}
