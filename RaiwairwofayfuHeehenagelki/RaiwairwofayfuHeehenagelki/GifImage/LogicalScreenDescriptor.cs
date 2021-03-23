
using System;

namespace RaiwairwofayfuHeehenagelki.GifImage
{

    /// <summary>
    ///     逻辑屏幕标识符(Logical Screen Descriptor)
    /// </summary>
    internal class LogicalScreenDescriptor
    {
        /// <summary>
        ///     逻辑屏幕宽度 像素数，定义GIF图象的宽度
        /// </summary>
        internal short Width { get; set; }

        /// <summary>
        ///     逻辑屏幕高度 像素数，定义GIF图象的高度
        /// </summary>
        internal short Height { get; set; }

        internal byte Packed { get; set; }

        /// <summary>
        ///     背景色,背景颜色(在全局颜色列表中的索引，如果没有全局颜色列表，该值没有意义)
        /// </summary>
        internal byte BgColorIndex { get; set; }

        /// <summary>
        ///     像素宽高比,像素宽高比(Pixel Aspect Radio)
        /// </summary>
        internal byte PixcelAspect { get; set; }

        /// <summary>
        ///     m - 全局颜色列表标志(Global Color Table Flag)，当置位时表示有全局颜色列表，pixel值有意义.
        /// </summary>
        internal bool GlobalColorTableFlag { get; set; }

        /// <summary>
        ///     cr - 颜色深度(Color ResoluTion)，cr+1确定图象的颜色深度.
        /// </summary>
        internal byte ColorResoluTion { get; set; }

        /// <summary>
        ///     s - 分类标志(Sort Flag)，如果置位表示全局颜色列表分类排列.
        /// </summary>
        internal int SortFlag { get; set; }

        /// <summary>
        ///     全局颜色列表大小，pixel+1确定颜色列表的索引数（2的pixel+1次方）.
        /// </summary>
        internal int GlobalColorTableSize { get; set; }


        internal byte[] GetBuffer()
        {
            var buffer = new byte[7];
            Array.Copy(BitConverter.GetBytes(Width), 0, buffer, 0, 2);
            Array.Copy(BitConverter.GetBytes(Height), 0, buffer, 2, 2);
            var m = 0;
            if (GlobalColorTableFlag)
            {
                m = 1;
            }

            var pixel = (byte) (Math.Log(GlobalColorTableSize, 2) - 1);
            Packed = (byte) (pixel | (SortFlag << 4) | (ColorResoluTion << 5) | (m << 7));
            buffer[4] = Packed;
            buffer[5] = BgColorIndex;
            buffer[6] = PixcelAspect;
            return buffer;
        }
    }

}