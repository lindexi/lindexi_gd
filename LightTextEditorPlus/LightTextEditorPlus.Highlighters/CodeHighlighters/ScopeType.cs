namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

/// <summary>
/// 表示代码高亮使用的语义范围类型。
/// </summary>
public enum ScopeType
{
    /// <summary>
    /// 注释。
    /// </summary>
    Comment,

    /// <summary>
    /// 类型名称。
    /// </summary>
    ClassName,

    /// <summary>
    /// 类型成员名称。
    /// </summary>
    ClassMember,

    /// <summary>
    /// 关键字。
    /// </summary>
    Keyword,

    /// <summary>
    /// 普通文本。
    /// </summary>
    PlainText,

    /// <summary>
    /// 字符串文本。
    /// </summary>
    String,

    /// <summary>
    /// 数值文本。
    /// </summary>
    Number,

    /// <summary>
    /// 括号或分隔符。
    /// </summary>
    Brackets,

    /// <summary>
    /// 局部变量或参数。
    /// </summary>
    Variable,
    /// <summary>
    /// 调用的方法
    /// </summary>
    Invocation,
    /// <summary>
    /// 变量/成员的类型定义，如 `var n` 中的 `var` 关键字
    /// </summary>
    DeclarationTypeSyntax,
}