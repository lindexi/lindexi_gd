using System.Collections.Generic;

namespace RaiwairwofayfuHeehenagelki.GifImage
{
    /// <summary>
    ///     注释扩展(Comment Extension)
    ///     这一部分是可选的（需要89a版本），可以用来记录图形、版权、描述等任何的非图形和控制
    ///     的纯文本数据(7-bit ASCII字符)，注释扩展并不影响对图象数据流的处理，解码器完全可以忽
    ///     略它。存放位置可以是数据流的任何地方，最好不要妨碍控制和数据块，推荐放在数据流的开始或结尾
    /// </summary>
    internal class CommentEx
    {
        /// <summary>
        ///     Comment Data - 一个或多个数据块组成
        /// </summary>
        internal List<string> CommentData { set; get; }
    }
}