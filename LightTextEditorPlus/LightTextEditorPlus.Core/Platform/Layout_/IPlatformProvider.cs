using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 用于提供各个平台的不同方式的接入
/// </summary>
public interface IPlatformProvider
{
    // 获取默认字体

    /// <summary>
    /// 加入调度更新布局请求
    /// </summary>
    /// 推荐处理：快速多次触发时，只触发一次，以及调度到合适的时机去执行
    /// <param name="textLayout"></param>
    void RequireDispatchUpdateLayout(Action textLayout);

    /// <summary>
    /// 创建文本日志
    /// </summary>
    /// <returns>可为空，为空采用空白日志</returns>
    ITextLogger? BuildTextLogger();

    IRunParagraphSplitter GetRunParagraphSplitter();

    /// <summary>
    /// 获取整行的 Run 的测量器，返回空则采用默认的测量逻辑
    /// </summary>
    /// <remarks>需要处理横竖排等布局方式</remarks>
    /// <returns></returns>
    IWholeLineLayouter? GetWholeRunLineLayouter();

    /// <summary>
    /// 获取文本的行测量器，返回空则采用默认的行测量逻辑
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
    /// 获取文本的字符测量器，返回空则采用默认的字符测量逻辑
    /// </summary>
    /// <returns></returns>
    ICharInfoMeasurer? GetCharInfoMeasurer();
}