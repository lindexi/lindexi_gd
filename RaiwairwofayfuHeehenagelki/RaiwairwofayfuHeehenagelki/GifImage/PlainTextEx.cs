using System;
using System.Collections.Generic;

namespace RaiwairwofayfuHeehenagelki.GifImage
{
    /// <summary>
    ///     形文本扩展(Plain Text Extension)这一部分是可选的（需要89a版本），
    ///     用来绘制一个简单的文本图象，这一部分由用来绘制的纯文本数据（7-bit ASCII字符）
    ///     和控制绘制的参数等组成。绘制文本借助于一个文本框（Text Grid）来定义边界，在文
    ///     本框中划分多个单元格，每个字符占用一个单元，绘制时按从左到右、从上到下的顺序
    ///     依次进行，直到最后一个字符或者占满整个文本框（之后的字符将被忽略，因此定义文
    ///     本框的大小时应该注意到是否可以容纳整个文本），绘制文本的颜色使用全局颜色列表，
    ///     没有则可以使用一个已经保存的前一个颜色列表。另外，图形文本扩展块也属于图形块
    ///     (Graphic Rendering Block)，可以在它前面定义图形控制扩展对它的表现形式进一步修改。
    /// </summary>
    internal struct PlainTextEx
    {
        #region 结构字段       

        /// <summary>
        ///     Block Size - 块大小，固定值12
        /// </summary>
        internal static readonly byte BlockSize = 0X0C;

        /// <summary>
        ///     Text Glid Left Posotion - 像素值，文本框离逻辑屏幕的左边界距
        /// </summary>
        internal short XOffSet;

        /// <summary>
        ///     Text Glid Top Posotion - 像素值，文本框离逻辑屏幕的上边界距离
        /// </summary>
        internal short YOffSet;

        /// <summary>
        ///     文本框高度 Text Glid Width -像素值
        /// </summary>
        internal short Width;

        /// <summary>
        ///     文本框高度 Text Glid Height - 像素值
        /// </summary>
        internal short Height;

        /// <summary>
        ///     字符单元格宽度 Character Cell Width - 像素值，单个单元格宽度
        /// </summary>
        internal byte CharacterCellWidth;

        /// <summary>
        ///     字符单元格高度 Character Cell Height- 像素值，单个单元格高度
        /// </summary>
        internal byte CharacterCellHeight;

        /// <summary>
        ///     文本前景色索引 Text Foreground Color Index - 前景色在全局颜色列表中的索引
        /// </summary>
        internal byte ForegroundColorIndex;

        /// <summary>
        ///     文本背景色索引 Text Blackground Color Index - 背景色在全局颜色列表中的索引
        /// </summary>
        internal byte BgColorIndex;

        /// <summary>
        ///     文本数据块集合Plain Text Data - 一个或多个数据块(Data Sub-Blocks)组成，保存要在显示的字符串。
        /// </summary>
        internal List<string> TextDatas;

        #endregion

        #region 方法函数

        internal byte[] GetBuffer()
        {
            var list = new List<byte>();
            list.Add(GifExtensions.ExtensionIntroducer);
            list.Add(GifExtensions.PlainTextLabel);
            list.Add(BlockSize);
            list.AddRange(BitConverter.GetBytes(XOffSet));
            list.AddRange(BitConverter.GetBytes(YOffSet));
            list.AddRange(BitConverter.GetBytes(Width));
            list.AddRange(BitConverter.GetBytes(Height));
            list.Add(CharacterCellWidth);
            list.Add(CharacterCellHeight);
            list.Add(ForegroundColorIndex);
            list.Add(BgColorIndex);
            if (TextDatas != null)
            {
                foreach (var text in TextDatas)
                {
                    list.Add((byte) text.Length);
                    foreach (var c in text)
                    {
                        list.Add((byte) c);
                    }
                }
            }

            list.Add(GifExtensions.Terminator);
            return list.ToArray();
        }

        #endregion
    }
}