// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkClipboardFormatsPrivate
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkClipboardFormatsPrivate
  {
    ICF_None = 0,
    ICF_InkSerializedFormat = 1,
    ICF_SketchInk = 2,
    ICF_TextInk = 6,
    ICF_PasteMask = 7,
    ICF_EnhancedMetafile = 8,
    ICF_Metafile = 32, // 0x00000020
    ICF_Bitmap = 64, // 0x00000040
    ICF_CopyMask = 127, // 0x0000007F
    ICF_Default = 127, // 0x0000007F
  }
}
