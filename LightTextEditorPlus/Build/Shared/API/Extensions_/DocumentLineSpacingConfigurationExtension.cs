#if USE_WPF || USE_AVALONIA
using LightTextEditorPlus.Core;

namespace LightTextEditorPlus;

/// <inheritdoc cref="LightTextEditorPlus.Core.DocumentLineSpacingConfigurationExtension"/>
public static class DocumentLineSpacingConfigurationExtension
{
    /// <inheritdoc cref="LightTextEditorPlus.Core.DocumentLineSpacingConfigurationExtension.UsePptLineSpacingStyle"/>
    public static void UsePptLineSpacingStyle(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.UsePptLineSpacingStyle();
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.DocumentLineSpacingConfigurationExtension.UseWpfLineSpacingStyle"/>
    public static void UseWpfLineSpacingStyle(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.UseWpfLineSpacingStyle();
    }

    /// <inheritdoc cref="LightTextEditorPlus.Core.DocumentLineSpacingConfigurationExtension.UseWordLineSpacingStrategy"/>
    public static void UseWordLineSpacingStrategy(this TextEditor textEditor)
    {
        textEditor.TextEditorCore.UseWordLineSpacingStrategy();
    }
}
#endif
