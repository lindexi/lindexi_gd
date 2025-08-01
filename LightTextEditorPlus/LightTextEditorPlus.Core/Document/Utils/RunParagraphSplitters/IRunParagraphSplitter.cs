using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Document.Utils;

/// <summary>
/// 提供将给定的 <see cref="IImmutableRun"/> 进行分段的功能的分离器。由于一个 <see cref="IImmutableRun"/> 可以是 `123\r\n123` 的内容，需要在底层分为两个不同的段
/// </summary>
public interface IRunParagraphSplitter
{
    /// <summary>
    /// 将传入的 <paramref name="run"/> 按照段落分割，每次返回的都是一个新段落。如果是换行开头的，那先返回一个内容为空的 <see cref="IImmutableRun"/> 元素，再返回后续的内容
    /// </summary>
    /// <param name="run"></param>
    /// <returns></returns>
    IEnumerable<IImmutableRun> Split(IImmutableRun run);
}