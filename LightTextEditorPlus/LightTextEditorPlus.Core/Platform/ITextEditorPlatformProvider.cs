using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Document.Utils;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
///     用于提供各个平台的不同方式的接入
/// </summary>
/// 平台接入属于框架的一部分，不同平台的接入方式可能不同，但是框架的核心逻辑是一致的
/// 平台接入直接耦合框架的实现方式。平台接入不属于抽象实现，只是将部分实现放在具体平台中。甚至可以认为后续是具体平台直接源代码引用框架代码，原本就是一套代码，只是被分开了
public interface ITextEditorPlatformProvider
{
    /// <summary>
    /// 获取文本的撤销恢复提供，仅构造调用一次
    /// </summary>
    /// <returns></returns>
    ITextEditorUndoRedoProvider BuildTextEditorUndoRedoProvider();

    /// <summary>
    /// 获取平台相关的字符属性创建器
    /// </summary>
    /// <returns></returns>
    IPlatformRunPropertyCreator GetPlatformRunPropertyCreator();

    /// <summary>
    ///     加入调度更新布局请求
    /// </summary>
    /// 推荐处理：快速多次触发时，只触发一次，以及调度到合适的时机去执行
    /// <param name="updateLayoutAction"></param>
    void RequireDispatchUpdateLayout(Action updateLayoutAction);

    /// <summary>
    /// 立刻执行更新布局。如果之前有调用 <see cref="RequireDispatchUpdateLayout"/> 请求布局，在此执行之后，将忽略
    /// </summary>
    /// <param name="updateLayoutAction"></param>
    void InvokeDispatchUpdateLayout(Action updateLayoutAction);

    /// <summary>
    ///     创建文本日志
    /// </summary>
    /// <returns>可为空，为空采用空白日志</returns>
    ITextLogger? BuildTextLogger();

    /// <summary>
    ///     获取对文本的分段分离器
    /// </summary>
    /// <returns></returns>
    IRunParagraphSplitter GetRunParagraphSplitter();

    /// <summary>
    ///     获取整行的 Run 的测量器，返回空则采用默认的测量逻辑
    /// </summary>
    /// <remarks>需要处理横竖排等布局方式</remarks>
    /// <returns></returns>
    /// <see cref="GetWholeRunLineLayouter"/> 包含 <see cref="GetWholeLineCharsLayouter"/>
    IWholeLineLayouter? GetWholeRunLineLayouter();

    /// <summary>
    ///    获取整行的字符的布局器。整行的字符布局器，用来布局一整行的字符，不包括行距等信息。只有字符排列
    /// </summary>
    /// <remarks>需要处理横竖排等布局方式</remarks>
    /// <returns></returns>
    IWholeLineCharsLayouter? GetWholeLineCharsLayouter();

    /// <summary>
    ///     获取文本的行测量器，返回空则采用默认的行测量逻辑。如自定义，则无须再处理 <see cref="GetCharInfoMeasurer"/> 返回的 <see cref="ICharInfoMeasurer"/> 的实现
    /// </summary>
    /// <remarks>需要处理横竖排等布局方式</remarks>
    /// <returns></returns>
    ISingleCharInLineLayouter? GetSingleRunLineLayouter();

    ///// <summary>
    ///// 获取字符的行测量器，用来测量哪些字符可以加入到当前行。返回空则采用默认的行测量逻辑
    ///// </summary>
    ///// <remarks>需要处理横竖排等布局方式</remarks>
    ///// <returns></returns>
    //ISingleCharInLineLayouter? GetSingleCharInLineLayouter();

    /// <summary>
    ///     获取文本的字符测量器，返回空则采用默认的字符测量逻辑
    /// </summary>
    /// <returns></returns>
    ICharInfoMeasurer? GetCharInfoMeasurer();

    /// <summary>
    ///     获取空段落的行高测量
    /// </summary>
    /// <returns></returns>
    IEmptyParagraphLineHeightMeasurer? GetEmptyParagraphLineHeightMeasurer();

    /// <summary>
    ///     获取渲染管理器
    /// </summary>
    /// <returns></returns>
    IRenderManager? GetRenderManager();

    /// <summary>
    /// 根据传入的字符属性获取字符行距
    /// </summary>
    /// <param name="runProperty"></param>
    /// <returns></returns>
    double GetFontLineSpacing(IReadOnlyRunProperty runProperty);

    /// <summary>
    /// 获取行距计算
    /// </summary>
    /// <returns></returns>
    ILineSpacingCalculator? GetLineSpacingCalculator();

    /// <summary>
    /// 获取平台字体名称管理器
    /// </summary>
    /// <returns></returns>
    IPlatformFontNameManager GetPlatformFontNameManager();

    /// <summary>
    /// 获取单词分隔器，分词器
    /// </summary>
    /// <returns></returns>
    IWordDivider GetWordDivider();
}
