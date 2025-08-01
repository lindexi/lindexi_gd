using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Rendering;

/// <summary>
/// 字符的四线格信息
/// </summary>
/*
   拼音的四线三格是从上往下数的，从上往下的四条线依次为：第一线、第二线、第三线、第四线；从上往下的三个格子依次为上格、中格、下格。

   在英文四线格的写法中，四条线分别有特定的名称，用来指导正确的笔迹书写。它们分别是：

   顶部线（Top Line）：这是最上面的一条线。大写字母和一些字母的上半部分会到达这条线。

   中线（Middle Line）：这条线位于顶部线和基线之间。小写字母如“a”、“c”、“e” 等的顶部会达到这条线。

   基线（Baseline）：这是写字的主要参考线，绝大多数字母都会停在这条线上。

   底线（Bottom Line）：这是最下面的一条线，用来指导字母的下方部分，例如“g”、“j”、“p”等。

   为了让代码具备更多适配性，这里取名参考英文的四线格（English handwriting paper）。同时基线（Baseline）可以更好对应排版信息

   参考文档：

   《人教版拼音四线三格写法标准》

   《四线格儿歌》

   《冒号和比号的体式及其应用问题》 林穗芳 人民出版社 2008
 */
public readonly record struct CharHandwritingPaperInfo
{
    /// <summary>
    /// 关联的文本编辑器
    /// </summary>
    public required TextEditorCore AssociatedTextEditor { get; init; }

    /// <summary>
    /// 顶部线（Top Line）：这是最上面的一条线。大写字母和一些字母的上半部分会到达这条线。对应拼音的四线三格的第一线。坐标相对于文本框
    /// </summary>
    public required double TopLineGradation { get; init; }

    /// <summary>
    /// 中线（Middle Line）：这条线位于顶部线和基线之间。小写字母如“a”、“c”、“e” 等的顶部会达到这条线。对应拼音的四线三格的第二线。坐标相对于文本框
    /// </summary>
    public required double MiddleLineGradation { get; init; }

    /// <summary>
    /// 基线（Baseline）：这是写字的主要参考线，绝大多数字母都会停在这条线上。对应拼音的四线三格的第三线。坐标相对于文本框
    /// </summary>
    public required double BaselineGradation { get; init; }

    /// <summary>
    /// 底线（Bottom Line）：这是最下面的一条线，用来指导字母的下方部分，例如“g”、“j”、“p”等。对应拼音的四线三格的第四线。坐标相对于文本框
    /// </summary>
    public required double BottomLineGradation { get; init; }
}
