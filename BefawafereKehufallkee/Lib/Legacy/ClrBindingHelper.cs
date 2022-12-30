using System.ComponentModel;

namespace BefawafereKehufallkee;

public class ClrBindingHelper
{
    /// <summary>
    /// 设置源到目标的单向绑定
    /// </summary>
    /// <param name="source">数据源，通常指底层提供的数据类</param>
    /// <param name="sourcePropertyPath">数据源中，属性的路径（名称）</param>
    /// <param name="target">目标，通常指 ViewModel，需要跟随源的变化而更新数据</param>
    /// <param name="targetPropertyPath">目标类中，属性的路径（名称）</param>
    public static void SetBindingByOneWay(
        object source, string sourcePropertyPath,
        object target, string targetPropertyPath)
    {
        BidirectionalBindingOperations.SetBinding(target, targetPropertyPath, new BidirectionalBinding(source, sourcePropertyPath)
        {
            Direction = BindingDirection.OneWay,
            InitMode = BindingInitMode.SourceToTarget
        });
    }
}