namespace WhefallralajaHubeanerelair;

internal enum InkRasterOperation
{
    IRO_Black = 1,
    IRO_NotMergePen = 2,
    IRO_MaskNotPen = 3,
    IRO_NotCopyPen = 4,
    IRO_MaskPenNot = 5,
    IRO_Not = 6,
    IRO_XOrPen = 7,
    IRO_NotMaskPen = 8,
    IRO_MaskPen = 9,
    IRO_NotXOrPen = 10, // 0x0000000A
    IRO_NoOperation = 11, // 0x0000000B
    IRO_MergeNotPen = 12, // 0x0000000C
    IRO_CopyPen = 13, // 0x0000000D
    IRO_MergePenNot = 14, // 0x0000000E
    IRO_MergePen = 15, // 0x0000000F
    IRO_White = 16, // 0x00000010
}