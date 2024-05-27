#if HAS_UNO
using Windows.UI;
using Uno.UI.Xaml.Core;
#endif

namespace UnoHacker;

public static class Hacker
{
    public static void Do()
    {
#if HAS_UNO
        Console.WriteLine($"ContentRoots Count={Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator.ContentRoots.Count}");
        
        foreach (var contentRoot in Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator.ContentRoots)
        {
            if (contentRoot.VisualTree.RootElement is IRootElement rootElement)
            {
                rootElement.SetBackgroundColor(Colors.Transparent);
            }
        }
#endif
    }
}

