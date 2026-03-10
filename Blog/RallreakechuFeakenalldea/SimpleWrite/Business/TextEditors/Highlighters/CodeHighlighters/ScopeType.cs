namespace SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;

public enum ScopeType
{
    Comment,
    ClassName,
    ClassMember,
    Keyword,
    PlainText,
    String,
    Number,
    Brackets,
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