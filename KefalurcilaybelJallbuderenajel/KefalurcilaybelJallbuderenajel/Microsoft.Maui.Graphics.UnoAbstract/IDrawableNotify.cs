namespace Microsoft.Maui.Graphics.UnoAbstract;

public interface IDrawableNotify
{
    event EventHandler<ICanvas>? Draw;
}
