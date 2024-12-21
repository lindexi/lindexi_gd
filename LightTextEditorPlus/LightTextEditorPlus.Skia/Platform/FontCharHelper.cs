using LightTextEditorPlus.Document;
using SkiaSharp;

namespace LightTextEditorPlus.Platform;

static class FontCharHelper
{
    /// <summary>
    /// 获取排版的字高。非渲染字高
    /// </summary>
    /// <param name="renderingRunPropertyInfo"></param>
    /// <returns></returns>
    /// 详细计算方法请参阅 《Skia 字体信息属性.enbx》 文档
    public static float GetLayoutCharHeight(this in RenderingRunPropertyInfo renderingRunPropertyInfo)
    {
        SKFont skFont = renderingRunPropertyInfo.Font;
        var baselineY = -skFont.Metrics.Ascent;
        var enhance = 0f;
        // 有些字体的 Top 就是超过格子，不要补偿。如华文仿宋字体
        //if (baselineY < Math.Abs(skFont.Metrics.Top))
        //{
        //    enhance = Math.Abs(skFont.Metrics.Top) - baselineY;
        //}
        var height = /*skFont.Metrics.Leading + 有些字体的 Leading 是不参与排版的，越过的，属于上加。不能将其加入计算 */ baselineY + skFont.Metrics.Descent + enhance;
        // 同理 skFont.Metrics.Bottom 也是不应该使用的，可能下加是超过格子的
        return height;
    }
}
