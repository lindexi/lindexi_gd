namespace WejallkachawDadeawejearhuce.Inking.WpfInking;

public static class WpfInkCanvasProvider
{
    public static void RegisterWpfInkCanvasWindowCreator(Func<IWpfInkCanvasWindow> creator)
    {
        _creator = creator;
    }

    private static Func<IWpfInkCanvasWindow>? _creator;

    public static IWpfInkCanvasWindow CreateWpfInkCanvasWindow()
    {
        if (_creator != null)
        {
            return _creator();
        }

        throw new InvalidOperationException("WpfInkCanvasWindow creator is not registered.");
    }
}