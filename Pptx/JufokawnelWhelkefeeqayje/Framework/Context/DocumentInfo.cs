using DocumentFormat.OpenXml.Flatten.Contexts;
using dotnetCampus.OpenXmlUnitConverter;

namespace JufokawnelWhelkefeeqayje.Framework.Context
{
    /// <summary>
    /// 文档信息
    /// </summary>
    public class DocumentInfo
    {
        /// <summary>
        /// 创建文档信息
        /// </summary>
        /// <param name="creator">文档创建者</param>
        /// <param name="lastModifiedBy">最后的修改者，默认等于文档创建者</param>
        /// <param name="slideSize">文档的页面尺寸，默认是 1280x720 像素大小</param>
        /// <param name="title">文档标题，默认是 PowerPoint 演示文稿</param>
        /// <param name="applicationName">转换的应用名，存放为扩展信息，可不写</param>
        /// <param name="applicationVersion">转换的应用的版本，存放为扩展信息，可不写</param>
        public DocumentInfo(string creator, string? lastModifiedBy = null, SlideEmuSize? slideSize = null,
            string title = "PowerPoint 演示文稿", string? applicationName = null, string? applicationVersion = null)
        {
            Creator = creator;
            LastModifiedBy = lastModifiedBy ?? creator;
            SlideSize = slideSize ?? new SlideEmuSize(new Pixel(1280).ToEmu(), new Pixel(720).ToEmu());
            Title = title;
            ApplicationName = applicationName;
            ApplicationVersion = applicationVersion;
        }

        /// <summary>
        /// 文档创建者
        /// </summary>
        public string Creator { get; }

        /// <summary>
        /// 最后的修改者，默认等于文档创建者
        /// </summary>
        public string LastModifiedBy { get; }

        /// <summary>
        /// 文档的页面尺寸，默认是 1280x720 像素大小
        /// </summary>
        public SlideEmuSize SlideSize { get; }

        /// <summary>
        /// 文档标题，默认是 PowerPoint 演示文稿
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 转换的应用名，存放为扩展信息，可不写
        /// </summary>
        public string? ApplicationName { get; }

        /// <summary>
        /// 转换的应用的版本，存放为扩展信息，可不写
        /// </summary>
        public string? ApplicationVersion { get; }
    }
}
