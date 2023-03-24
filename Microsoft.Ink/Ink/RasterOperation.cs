// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.RasterOperation
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

namespace Microsoft.Ink
{
  public enum RasterOperation
  {
    Black = 1,
    NotMergePen = 2,
    MaskNotPen = 3,
    NotCopyPen = 4,
    MakePenNot = 5,
    Not = 6,
    XOrPen = 7,
    NotMaskPen = 8,
    MaskPen = 9,
    NotXOrPen = 10, // 0x0000000A
    NoOperation = 11, // 0x0000000B
    MergeNotPen = 12, // 0x0000000C
    CopyPen = 13, // 0x0000000D
    MergePenNot = 14, // 0x0000000E
    MergePen = 15, // 0x0000000F
    White = 16, // 0x00000010
  }
}
