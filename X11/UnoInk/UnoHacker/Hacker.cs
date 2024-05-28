
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;
#if HAS_UNO
using Windows.UI;
using Uno.UI.Xaml.Core;
#endif

namespace UnoHacker;

public static class ApplicationViewExtension
{
    public static ApplicationView GetApplicationView(this AppWindow appWindow) =>
        ApplicationView.GetForWindowId(appWindow.Id);
}

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

