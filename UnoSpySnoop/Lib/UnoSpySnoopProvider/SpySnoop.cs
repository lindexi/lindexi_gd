using Windows.Foundation;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using UnoSpySnoopDebugger.IpcCommunicationContext;

namespace UnoSpySnoop;

public class SpySnoop
{
    public static void StartSpyUI(Grid spySnoopRootGrid, string? debugIpcName = null)
    {
        debugIpcName ??= "UnoSpySnoop";

        var unoSpySnoop = new SpySnoop(spySnoopRootGrid, debugIpcName);
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
        AddRequestHandler(RoutedPathList.GetRootVisualTree, GetRootVisualTree);
        AddNotifyHandler<SelectElementRequest>(RoutedPathList.SelectElement, SelectElement);

        _jsonIpcDirectRoutedProvider.StartServer();
    }

    private void SelectElement(SelectElementRequest request)
    {
        _rootGrid.Children.Clear();

        if (_tokenDictionary.TryGetValue(request.ElementToken, out var weakReference)
            && weakReference.TryGetTarget(out var element))
        {
            if (element is FrameworkElement frameworkElement)
            {
                GeneralTransform transformToVisual = frameworkElement.TransformToVisual(_rootGrid);
                var rect = new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight);
                Rect newRect = transformToVisual.TransformBounds(rect);

                var rectangle = new Rectangle()
                {
                    RenderTransform = new TranslateTransform()
                    {
                        X = newRect.X,
                        Y = newRect.Y,
                    },

                    Width = newRect.Width,
                    Height = newRect.Height,

                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 2
                };
                _rootGrid.Children.Add(rectangle);
            }
        }
    }

    private void AddRequestHandler<TResponse>(string path, Func<TResponse> handler)
    {
        _jsonIpcDirectRoutedProvider.AddRequestHandler<TResponse>(path, () =>
        {
            var taskCompletionSource = new TaskCompletionSource<TResponse>();
            _rootGrid.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                try
                {
                    var result = handler();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });
            return taskCompletionSource.Task;
        });
    }

    private void AddNotifyHandler<TRequest>(string path, Action<TRequest> action)
    {
        _jsonIpcDirectRoutedProvider.AddNotifyHandler<TRequest>(path, args =>
        {
            _rootGrid.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                action(args);
            });
        });
    }

    private ElementProxy GetRootVisualTree()
    {
        var todRemovedToken = new List<string>();
        foreach ((string? key, WeakReference<DependencyObject>? value) in _tokenDictionary)
        {
            if (!value.TryGetTarget(out _))
            {
                todRemovedToken.Add(key);
            }
        }

        foreach (string token in todRemovedToken)
        {
            _tokenDictionary.Remove(token);
        }

        return ToElementProxy(_rootElement);
    }

    private ElementProxy ToElementProxy(DependencyObject element)
    {
        var token = GetOrCreateToken(element);
        string? name = null;
        if (element is FrameworkElement frameworkElement)
        {
            name = frameworkElement.Name;
        }

        var type = element.GetType();

        var elementBaseInfo = new ElementBaseInfo(token, type.Name, type.FullName!, name);
        var children = new List<ElementProxy>();

        var childrenCount = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < childrenCount; i++)
        {
            var dependencyObject = VisualTreeHelper.GetChild(element, i);
            var child = ToElementProxy(dependencyObject);
            children.Add(child);
        }

        if (children.Count == 0)
        {
            // 给一个空的好判断，也减少 json 的序列化
            children = null;
        }

        return new ElementProxy(elementBaseInfo, children);
    }

    private readonly Dictionary<string/*Token*/, WeakReference<DependencyObject> /*Element*/> _tokenDictionary = new();

    public static readonly DependencyProperty TokenProperty = DependencyProperty.RegisterAttached(
        "Token", typeof(string), typeof(SpySnoop), new PropertyMetadata(default(string)));

    public void SetToken(DependencyObject element, string value)
    {
        if (!_tokenDictionary.TryAdd(value, new WeakReference<DependencyObject>(element)))
        {
            throw new InvalidOperationException($"Should not add duplicate token");
        }

        element.SetValue(TokenProperty, value);
    }

    public string GetOrCreateToken(DependencyObject element)
    {
        var value = element.GetValue(TokenProperty) as string;
        if (value is null)
        {
            value = Guid.NewGuid().ToString();
            SetToken(element, value);
        }
        return value;
    }

    public static readonly DependencyProperty IsHighlightElementProperty = DependencyProperty.RegisterAttached("IsHighlightElement", typeof(bool), typeof(SpySnoop), new PropertyMetadata(default(bool)));

    public static void SetIsHighlightElement(DependencyObject element, bool value)
    {
        element.SetValue(IsHighlightElementProperty, value);
    }

    public static bool GetIsHighlightElement(DependencyObject element)
    {
        return (bool)element.GetValue(IsHighlightElementProperty);
    }
}
