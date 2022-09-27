using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本库使用的尺寸
/// </summary>
public readonly struct Size
{
    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width { get; }
    public double Height { get; }

    public static Size Zero => new Size(0, 0);
}