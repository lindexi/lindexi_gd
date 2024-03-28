using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;

namespace NujawkeqefunuyeBogearfagallnuhea.UnoSpySnoop;

public class SpySnoop
{
    public static void StartSpyUI(Grid rootGrid, string? debugIpcName = null)
    {
        debugIpcName ??= "UnoSpySnoop";

        var unoSpySnoop = new SpySnoop(rootGrid, debugIpcName);
        unoSpySnoop.Start();
    }

    private SpySnoop(Grid rootGrid, string debugIpcName)
    {
        _rootGrid = rootGrid;
        _debugIpcName = debugIpcName;
        _rootElement = GetRootElement();
        var ipcProvider = new IpcProvider(debugIpcName);
        _ipcProvider = ipcProvider;
        var jsonIpcDirectRoutedProvider = new JsonIpcDirectRoutedProvider(ipcProvider);
        _jsonIpcDirectRoutedProvider = jsonIpcDirectRoutedProvider;

        DependencyObject GetRootElement()
        {
            DependencyObject currentElement = rootGrid;

            while (true)
            {
                var parent = VisualTreeHelper.GetParent(currentElement);
                if (parent is null)
                {
                    break;
                }

                currentElement = parent;
            }

            return currentElement;
        }
    }

    private readonly Grid _rootGrid;

    private readonly string _debugIpcName;

    private readonly DependencyObject _rootElement;

    private readonly IpcProvider _ipcProvider;

    private readonly JsonIpcDirectRoutedProvider _jsonIpcDirectRoutedProvider;

    public void Start()
    {
        _jsonIpcDirectRoutedProvider.StartServer();
    }
}
