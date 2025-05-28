using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document.Decorations;

/// <summary>
/// 文本的装饰
/// </summary>
public interface ITextEditorDecoration
{
    /// <summary>
    /// 获取文本的装饰放在文本的哪里
    /// </summary>
    TextEditorDecorationLocation TextDecorationLocation { get; }
}
