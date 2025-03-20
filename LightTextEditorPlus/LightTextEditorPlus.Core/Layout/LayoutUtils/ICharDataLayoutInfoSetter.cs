using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

/// <summary>
/// 用于设置字符布局信息，这是一个辅助接口，核心只是为了让 <see cref="CharData"/> 不要开放一些方法而已。限定只有在布局的时候才能设置
/// </summary>
public interface ICharDataLayoutInfoSetter
{
    /// <inheritdoc cref="CharData.SetLayoutCharLineStartPoint"/>
    void SetLayoutStartPoint(CharData charData, TextPointInLineCoordinateSystem point /*, TextPoint baselineStartPoint*/);

    /// <inheritdoc cref="CharData.SetCharDataInfo"/>
    void SetCharDataInfo(CharData charData, TextSize textSize, double baseline);
}