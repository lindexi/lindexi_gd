// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkRecognizerCapabilities
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkRecognizerCapabilities
  {
    [TypeLibVar(64)] IRC_DontCare = 1,
    IRC_Object = 2,
    IRC_FreeInput = 4,
    IRC_LinedInput = 8,
    IRC_BoxedInput = 16, // 0x00000010
    IRC_CharacterAutoCompletionInput = 32, // 0x00000020
    IRC_RightAndDown = 64, // 0x00000040
    IRC_LeftAndDown = 128, // 0x00000080
    IRC_DownAndLeft = 256, // 0x00000100
    IRC_DownAndRight = 512, // 0x00000200
    IRC_ArbitraryAngle = 1024, // 0x00000400
    IRC_Lattice = 2048, // 0x00000800
    IRC_AdviseInkChange = 4096, // 0x00001000
    IRC_StrokeReorder = 8192, // 0x00002000
    IRC_Personalizable = 16384, // 0x00004000
    IRC_PrefersArbitraryAngle = 32768, // 0x00008000
    IRC_PrefersParagraphBreaking = 65536, // 0x00010000
    IRC_PrefersSegmentation = 131072, // 0x00020000
    IRC_Cursive = 262144, // 0x00040000
    IRC_TextPrediction = 524288, // 0x00080000
    IRC_Alpha = 1048576, // 0x00100000
    IRC_Beta = 2097152, // 0x00200000
  }
}
