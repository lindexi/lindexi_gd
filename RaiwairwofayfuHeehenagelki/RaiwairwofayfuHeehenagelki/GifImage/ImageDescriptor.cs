using System;
using System.Collections.Generic;

namespace RaiwairwofayfuHeehenagelki.GifImage
{
    /// <summary>
    ///     图象标识符(Image Descriptor)一个GIF文件内可以包含多幅图象，
    ///     一幅图象结束之后紧接着下是一幅图象的标识符，图象标识符以0x2C(',')
    ///     字符开始，定义紧接着它的图象的性质，包括图象相对于逻辑屏幕边界的偏移量、
    ///     图象大小以及有无局部颜色列表和颜色列表大小，由10个字节组成
    /// </summary>
    internal class ImageDescriptor
    {

        internal byte[] GetBuffer()
        {
            var list = new List<byte> { GifExtensions.ImageDescriptorLabel };
            list.AddRange(BitConverter.GetBytes(XOffSet));
            list.AddRange(BitConverter.GetBytes(YOffSet));
            list.AddRange(BitConverter.GetBytes(Width));
            list.AddRange(BitConverter.GetBytes(Height));
            var m = 0;
            if (LocalColorTableFlag)
            {
                m = 1;
            }

            var i = 0;
            if (InterlaceFlag)
            {
                i = 1;
            }

            var s = 0;
            if (SortFlag)
            {
                s = 1;
            }

            var pixel = (byte) (Math.Log(LocalColorTableSize, 2) - 1);
            var packed = (byte) (pixel | (s << 5) | (i << 6) | (m << 7));
            list.Add(packed);
            return list.ToArray();
        }


        #region 结构字段      

        /// <summary>
        ///     X方向偏移量
        /// </summary>
        internal short XOffSet { set; get; }

        /// <summary>
        ///     X方向偏移量
        /// </summary>
        internal short YOffSet { set; get; }

        /// <summary>
        ///     图象宽度
        /// </summary>
        internal short Width { set; get; }

        /// <summary>
        ///     图象高度
        /// </summary>
        internal short Height { set; get; }

        /// <summary>
        ///     packed
        /// </summary>
        internal byte Packed { set; get; }

        /// <summary>
        ///     局部颜色列表标志(Local Color Table Flag)
        ///     置位时标识紧接在图象标识符之后有一个局部颜色列表，供紧跟在它之后的一幅图象使用；
        ///     值否时使用全局颜色列表，忽略pixel值。
        /// </summary>
        internal bool LocalColorTableFlag { set; get; }

        /// <summary>
        ///     交织标志(Interlace Flag)，置位时图象数据使用连续方式排列，否则使用顺序排列。
        /// </summary>
        internal bool InterlaceFlag { set; get; }

        /// <summary>
        ///     分类标志(Sort Flag)，如果置位表示紧跟着的局部颜色列表分类排列.
        /// </summary>
        internal bool SortFlag { set; get; }

        /// <summary>
        ///     pixel - 局部颜色列表大小(Size of Local Color Table)，pixel+1就为颜色列表的位数
        /// </summary>
        internal int LocalColorTableSize { set; get; }

        #endregion
    }
}