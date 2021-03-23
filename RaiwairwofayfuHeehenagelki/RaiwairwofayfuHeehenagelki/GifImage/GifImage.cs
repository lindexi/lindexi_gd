
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace RaiwairwofayfuHeehenagelki.GifImage
{

    /// <summary>
    ///     类GifImage - 描述Gif的类
    ///     感谢 jillzhang 提供算法
    /// </summary>
    internal class GifImage
    {
        #region 背景图片的长度 

        /// <summary>
        ///     背景图片的长度
        /// </summary>
        internal short Width => LogicalScreenDescriptor.Width;

        #endregion

        #region 背景图片的高度

        /// <summary>
        ///     背景图片的高度
        /// </summary>
        internal short Height => LogicalScreenDescriptor.Height;

        #endregion

        #region Gif的调色板

        /// <summary>
        ///     Gif的调色板
        /// </summary>
        internal Color[] Palette
        {
            get
            {
                var act = PaletteHelper.GetColor32s(GlobalColorTable);
                act[LogicalScreenDescriptor.BgColorIndex] = Color.FromArgb(0,0,0,0);
                return act;
            }
        }

        #endregion

     
        /// <summary>
        ///    开始6字节是GIF署名 文件版本号，GIF署名 是“GIF”，文件版本号为 "87a"或"89a"
        /// </summary>
        internal string Header { get; set; } = "";

       

        #region 全局颜色列表

        /// <summary>
        ///     全局颜色列表
        /// </summary>
        internal byte[] GlobalColorTable { get; set; }

        #endregion

        #region 全局颜色的索引表

        /// <summary>
        ///     全局颜色的索引表
        /// </summary>
        internal Hashtable GlobalColorIndexedTable { get; } = new Hashtable();

        #endregion

        #region 注释扩展块集合

        /// <summary>
        ///     注释块集合
        /// </summary>
        internal List<CommentEx> CommentExtensions { get; set; } = new List<CommentEx>();

        #endregion

        #region 应用程序扩展块集合

        /// <summary>
        ///     应用程序扩展块集合
        /// </summary>
        internal List<ApplicationEx> ApplictionExtensions { get; set; } = new List<ApplicationEx>();

        #endregion

        #region 图形文本扩展集合

        /// <summary>
        ///     图形文本扩展集合
        /// </summary>
        internal List<PlainTextEx> PlainTextExtensions { get; set; } = new List<PlainTextEx>();

        #endregion

        #region 逻辑屏幕描述

        /// <summary>
        ///     逻辑屏幕描述
        /// </summary>
        internal LogicalScreenDescriptor LogicalScreenDescriptor { get; set; }

        #endregion

        #region 解析出来的帧集合

        /// <summary>
        ///     解析出来的帧集合
        /// </summary>
        internal List<GifFrame> Frames { get; set; } = new List<GifFrame>();

        #endregion
    }

}