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
    public bool TryHitTest(in TextPoint point, out TextHitTestResult result)
    {
        if (IsDirty
            // 非空文本的初始化状态下，不允许命中测试。如果在初始化状态下，允许命中测试，返回的就是文档末尾。此时也不需要申请布局
            && !IsEmptyInitializingTextEditor())
        {
            result = default;
            return false;
        }

        result = _layoutManager.HitTest(in point);
        return true;
    }

    /// <summary>
    /// 返回接近传入点的字符的索引。在文本库内，应该使用 <see cref="TryHitTest"/> 方法进行命中测试
    /// </summary>
    [Obsolete("请使用 TryHitTest 执行命中测试，此方法的存在仅仅只是为了告诉你正确的方法应该是 TryHitTest 方法", true)]
    public void GetCharacterIndexFromPoint()
    {
    }
}
