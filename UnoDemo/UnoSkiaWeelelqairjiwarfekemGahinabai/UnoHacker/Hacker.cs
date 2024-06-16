
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;
#if HAS_UNO
using Windows.UI;
using Uno.UI.Xaml.Core;
#endif

namespace UnoHacker;

public static class ApplicationViewExtension
{
#if HAS_UNO
    public static ApplicationView GetApplicationView(this AppWindow appWindow) =>
        ApplicationView.GetForWindowId(appWindow.Id);
#endif
}

public static class Hacker
{
    public static void Do()
    {
#if HAS_UNO
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

