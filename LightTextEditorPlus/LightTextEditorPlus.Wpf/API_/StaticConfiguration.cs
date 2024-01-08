using System.Windows.Media;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Exceptions;

namespace LightTextEditorPlus;

/// <summary>
/// 文本库的静态配置，请从 <see cref="TextEditor.StaticConfiguration"/> 进行配置。许多配置只能配置一次
/// </summary>
public class StaticConfiguration
{
    /// <summary>
    /// 业务端可以配置其用来处理字体回滚
    /// </summary>
    public FontNameManager FontNameManager { get; } = new FontNameManager();

    /// <summary>
    /// 默认的未定义的字体使用的字体。如果当前的字符属性等没有定义明确的字体，那就采用此字体
    /// </summary>
    public FontFamily DefaultNotDefineFontFamily
    {
        get => _defaultNotDefineFontFamily ??
               (_defaultFontFamily ??= new FontFamily("微软雅黑"));
        set
        {
            if (_defaultNotDefineFontFamily is not null)
            {
                StaticConfigurationPropertyMultipleSettingsException.Throw(nameof(DefaultNotDefineFontFamily));
            }

            _defaultNotDefineFontFamily = value;
        }
    }

    private FontFamily? _defaultFontFamily;
    private FontFamily? _defaultNotDefineFontFamily;
}