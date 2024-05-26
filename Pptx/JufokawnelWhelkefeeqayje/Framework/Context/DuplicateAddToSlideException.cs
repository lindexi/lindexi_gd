using DocumentFormat.OpenXml.Presentation;

namespace JufokawnelWhelkefeeqayje.Framework.Context
{
    public class DuplicateAddToSlideException : InvalidOperationException
    {
        public DuplicateAddToSlideException(Slide slide) : base($"元素不能重复加入到页面中")
        {
            Slide = slide;
        }

        /// <summary>
        /// 元素已加入的页面
        /// </summary>
        public Slide Slide { get; }
    }
}
