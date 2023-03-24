// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkApplicationGesture
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System.Security;

namespace Microsoft.Ink
{
  [SuppressUnmanagedCodeSecurity]
  internal enum InkApplicationGesture
  {
    IAG_AllGestures = 0,
    IAG_NoGesture = 61440, // 0x0000F000
    IAG_Scratchout = 61441, // 0x0000F001
    IAG_Triangle = 61442, // 0x0000F002
    IAG_Square = 61443, // 0x0000F003
    IAG_Star = 61444, // 0x0000F004
    IAG_Check = 61445, // 0x0000F005
    IAG_Curlicue = 61456, // 0x0000F010
    IAG_DoubleCurlicue = 61457, // 0x0000F011
    IAG_Circle = 61472, // 0x0000F020
    IAG_DoubleCircle = 61473, // 0x0000F021
    IAG_SemiCircleLeft = 61480, // 0x0000F028
    IAG_SemiCircleRight = 61481, // 0x0000F029
    IAG_ChevronUp = 61488, // 0x0000F030
    IAG_ChevronDown = 61489, // 0x0000F031
    IAG_ChevronLeft = 61490, // 0x0000F032
    IAG_ChevronRight = 61491, // 0x0000F033
    IAG_ArrowUp = 61496, // 0x0000F038
    IAG_ArrowDown = 61497, // 0x0000F039
    IAG_ArrowLeft = 61498, // 0x0000F03A
    IAG_ArrowRight = 61499, // 0x0000F03B
    IAG_Up = 61528, // 0x0000F058
    IAG_Down = 61529, // 0x0000F059
    IAG_Left = 61530, // 0x0000F05A
    IAG_Right = 61531, // 0x0000F05B
    IAG_UpDown = 61536, // 0x0000F060
    IAG_DownUp = 61537, // 0x0000F061
    IAG_LeftRight = 61538, // 0x0000F062
    IAG_RightLeft = 61539, // 0x0000F063
    IAG_UpLeftLong = 61540, // 0x0000F064
    IAG_UpRightLong = 61541, // 0x0000F065
    IAG_DownLeftLong = 61542, // 0x0000F066
    IAG_DownRightLong = 61543, // 0x0000F067
    IAG_UpLeft = 61544, // 0x0000F068
    IAG_UpRight = 61545, // 0x0000F069
    IAG_DownLeft = 61546, // 0x0000F06A
    IAG_DownRight = 61547, // 0x0000F06B
    IAG_LeftUp = 61548, // 0x0000F06C
    IAG_LeftDown = 61549, // 0x0000F06D
    IAG_RightUp = 61550, // 0x0000F06E
    IAG_RightDown = 61551, // 0x0000F06F
    IAG_Exclamation = 61604, // 0x0000F0A4
    IAG_Tap = 61680, // 0x0000F0F0
    IAG_DoubleTap = 61681, // 0x0000F0F1
  }
}
