namespace WhefallralajaHubeanerelair;

internal enum InkRasterOperation
{
    Black = 1,
    NotMergePen = 2,
    MaskNotPen = 3,
    NotCopyPen = 4,
    MaskPenNot = 5,
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