using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 项目符号
/// </summary>
public abstract class TextMarker
{
    public IReadOnlyRunProperty? RunProperty { get; init; }

    /// <summary>
    /// 用于限制程序集外继承的科技
    /// </summary>
    internal abstract void DisableInherit();
}