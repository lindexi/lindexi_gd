using System;

namespace LightTextEditorPlus;

internal class APIConstraintAttribute : Attribute
{
    /// <summary>
    /// API 约束
    /// </summary>
    /// <param name="constraintFileName">提供 API 约束的文件名</param>
    /// <param name="ignoreOrder">是否可忽略排序</param>
    public APIConstraintAttribute(string constraintFileName, bool ignoreOrder = false)
    {
        ConstraintFileName = constraintFileName;
        IgnoreOrder = ignoreOrder;
    }

    public string ConstraintFileName { get; }

    public bool IgnoreOrder { get; }
}