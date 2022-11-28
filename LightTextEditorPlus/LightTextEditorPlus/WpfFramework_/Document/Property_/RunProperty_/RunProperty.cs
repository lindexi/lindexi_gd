using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

public class RunProperty : LayoutOnlyRunProperty
{
    public RunProperty(RunProperty? styleRunProperty = null) : base(styleRunProperty)
    {
        StyleRunProperty = styleRunProperty;
    }

    /// <summary>
    /// 继承样式里的属性
    /// </summary>
    private LayoutOnlyRunProperty? StyleRunProperty { get; }
}