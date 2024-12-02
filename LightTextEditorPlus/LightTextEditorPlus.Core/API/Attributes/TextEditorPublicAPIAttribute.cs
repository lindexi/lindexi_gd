using System;

namespace LightTextEditorPlus.Core.Attributes;

/// <summary>
/// 表示这是一个公开的 API 专门给框架之外调用，非框架内使用。这个特性没有什么功能，只是用来给框架开发者了解某个 API 不应该在框架内调用
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Delegate | AttributeTargets.Event)]
public class TextEditorPublicAPIAttribute : Attribute
{
}
