using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Attributes;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core;

// 此文件存放状态获取相关的方法
public partial class TextEditorCore
{
    /// <summary>
    /// 尝试进行命中测试。如果文本没有完成布局，那将命中测试失败
    /// </summary>
    /// <param name="point"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    [TextEditorPublicAPI]
    public bool TryHitTest(in Point point, out TextHitTestResult result)
    {
        if (IsDirty)
        {
            result = default;
            return false;
        }

        result = _layoutManager.HitTest(point);
        return true;
    }
}
