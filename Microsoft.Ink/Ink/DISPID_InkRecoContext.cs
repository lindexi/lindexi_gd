// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.DISPID_InkRecoContext
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  [TypeLibType(16)]
  internal enum DISPID_InkRecoContext
  {
    DISPID_IRecoCtx_Strokes = 1,
    DISPID_IRecoCtx_CharacterAutoCompletionMode = 2,
    DISPID_IRecoCtx_Factoid = 3,
    DISPID_IRecoCtx_WordList = 4,
    DISPID_IRecoCtx_Recognizer = 5,
    DISPID_IRecoCtx_Guide = 6,
    DISPID_IRecoCtx_Flags = 7,
    DISPID_IRecoCtx_PrefixText = 8,
    DISPID_IRecoCtx_SuffixText = 9,
    DISPID_IRecoCtx_StopRecognition = 10, // 0x0000000A
    DISPID_IRecoCtx_Clone = 11, // 0x0000000B
    DISPID_IRecoCtx_Recognize = 12, // 0x0000000C
    DISPID_IRecoCtx_StopBackgroundRecognition = 13, // 0x0000000D
    DISPID_IRecoCtx_EndInkInput = 14, // 0x0000000E
    DISPID_IRecoCtx_BackgroundRecognize = 15, // 0x0000000F
    DISPID_IRecoCtx_BackgroundRecognizeWithAlternates = 16, // 0x00000010
    DISPID_IRecoCtx_IsStringSupported = 17, // 0x00000011
  }
}
