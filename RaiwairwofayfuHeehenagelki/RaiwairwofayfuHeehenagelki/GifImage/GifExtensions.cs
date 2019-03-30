
namespace RaiwairwofayfuHeehenagelki.GifImage
{


    internal static class GifExtensions
    {
        /// <summary>
        ///     Extension Introducer 
        /// 图形控制扩展 放在一个图象块(图象标识符)或文本扩展块的前面，用来控制跟在它后面的第一个图象
        /// </summary>
        internal const byte ExtensionIntroducer = 0x21;

        /// <summary>
        ///     lock Terminator 
        /// </summary>
        internal const byte Terminator = 0;


        /// <summary>
        ///     Application Extension Label 
        /// </summary>
        internal const byte ApplicationExtensionLabel = 0xFF;


        /// <summary>
        ///     Comment Label
        /// </summary>
        internal const byte CommentLabel = 0xFE;


        /// <summary>
        /// 一个 Gif 包括很多个图片，使用 0x2C 说明图片开始
        /// </summary>
        internal const byte ImageDescriptorLabel = ImageLabel;

        /// <summary>
        ///     Plain Text Label
        /// </summary>
        internal const byte PlainTextLabel = 0x01;

        /// <summary>
        ///     Graphic Control Label 
        /// </summary>
        internal const byte GraphicControlLabel = 0xF9;

        /// <summary>
        /// 一个 Gif 包括很多个图片，使用 0x2C 说明图片开始
        /// </summary>
        internal const byte ImageLabel = 0x2C;

       /// <summary>
        /// 标识GIF文件结束
       /// </summary>
        internal const byte EndIntroducer = 0x3B;
    }

}