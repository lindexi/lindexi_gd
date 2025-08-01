//using System.Diagnostics.CodeAnalysis;
//using LightTextEditorPlus.Core.Document;
//using LightTextEditorPlus.Core.Platform;
//using LightTextEditorPlus.Document;
//using SkiaSharp;

//namespace LightTextEditorPlus.Platform;

///// <summary>
///// 字体名称到 SKTypeface 管理器
///// </summary>
///// 字体回滚有两层：
///// 第一层是用户传入的 UserFontName 找不到时的，字体名回滚策略
///// 第二层是传入的字体无法处理对应字符时的回滚策略
//public interface IFontNameToSKTypefaceManager
//{
//    /// <summary>
//    /// 解析字体。在此方法里面，应该处理字体找不到的情况，进行字体名回滚策略
//    /// </summary>
//    /// <param name="runProperty"></param>
//    /// <returns></returns>
//    SKTypeface Resolve(SkiaTextRunProperty runProperty);

//    ///// <summary>
//    ///// 尝试回滚字体属性。在此方法里面，应该处理字体无法处理对应字符的情况，进行字体回滚策略
//    ///// </summary>
//    ///// <param name="runProperty"></param>
//    ///// <param name="charObject"></param>
//    ///// <param name="newRunProperty"></param>
//    ///// <returns></returns>
//    //bool TryFallbackRunProperty(SkiaTextRunProperty runProperty, ICharObject charObject,
//    //    [NotNullWhen(true)] out SkiaTextRunProperty? newRunProperty);
//}
