using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Configurations;

public readonly record struct SkiaTextEditorRenderConfiguration()
{
    /// <summary>
    /// 使用逐字渲染方法。渲染效率慢，但可以遵循布局结果
    /// </summary>
    public bool UseRenderCharByCharMode { get; init; } = false;
}
