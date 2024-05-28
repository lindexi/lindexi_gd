using DocumentFormat.OpenXml.Packaging;

namespace JufokawnelWhelkefeeqayje.Framework.Context
{
    public readonly struct BitmapSourceToImagePartResult
    {
        /// <summary>
        /// 创建从 <see cref="BitmapSourcePresentationProxy"/> 转换为 <see cref="DocumentFormat.OpenXml.Packaging.ImagePart"/> 的结果
        /// </summary>
        /// <param name="id">存放到容器的序号</param>
        /// <param name="imagePart">转换的 <see cref="DocumentFormat.OpenXml.Packaging.ImagePart"/> 的结果</param>
        public BitmapSourceToImagePartResult(string id, ImagePart imagePart)
        {
            Id = id; 
            ImagePart = imagePart;
        }

        /// <summary>
        /// 存放到容器的序号
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 转换的 <see cref="DocumentFormat.OpenXml.Packaging.ImagePart"/> 的结果
        /// </summary>
        public ImagePart ImagePart { get; }
    }
}
