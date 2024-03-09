using System.CodeDom.Compiler;
using System.Collections.ObjectModel;

using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Data;

using UnoSpySnoopDebugger.IpcCommunicationContext;

using Windows.Media.Protection.PlayReady;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
// Copy from https://github.com/snoopwpf/snoopwpf

namespace UnoSpySnoopDebugger.View;

public sealed partial class SnoopUserControl : UserControl
{
    public SnoopUserControl(JsonIpcDirectRoutedClientProxy client)
    {
        Client = client;
        this.InitializeComponent();
    }

    public JsonIpcDirectRoutedClientProxy Client { get; }

    public static readonly DependencyProperty CurrentElementTreeProperty = DependencyProperty.Register(
        nameof(CurrentElementTree), typeof(List<ElementProxy>), typeof(SnoopUserControl), new PropertyMetadata(default(List<ElementProxy>)));

    public List<ElementProxy> CurrentElementTree
    {
        get { return (List<ElementProxy>) GetValue(CurrentElementTreeProperty); }
        set { SetValue(CurrentElementTreeProperty, value); }
    }

    private ElementProxy _rootElement = null!;

    public async Task StartAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var elementProxy = await Client.GetResponseAsync<ElementProxy>(RoutedPathList.GetRootVisualTree);
        _rootElement = elementProxy!;
        CurrentElementTree = new List<ElementProxy>(1) { _rootElement };
    }

    private void ElementTreeView_OnItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is ElementProxy element)
        {
            _ = Client.NotifyAsync(RoutedPathList.SelectElement, new SelectElementRequest(element.ElementInfo.Token));
            CurrentElement = element;
        }
    }

    private ElementProxy? CurrentElement
    {
        set
        {
            _currentElement = value;
            if (value is not null)
            {
                _ = UpdateElementPropertyList(value);
            }
        }
        get => _currentElement;
    }

    private ElementProxy? _currentElement;

    private async Task UpdateElementPropertyList(ElementProxy element)
    {
        var response = await Client.GetResponseAsync<GetElementPropertyResponse>(RoutedPathList.GetElementPropertyList, new GetElementPropertyRequest(element.ElementInfo.Token));
        if (response == null)
        {
            return;
        }

        List<DependencyPropertyInfo> list = response.DependencyPropertyInfoList;
        list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        DependencyPropertyInfoList.Clear();

        foreach (var propertyInfo in list)
        {
            DependencyPropertyInfoList.Add(propertyInfo);
        }
    }

    public ObservableCollection<DependencyPropertyInfo> DependencyPropertyInfoList { get; } =
        new ObservableCollection<DependencyPropertyInfo>();
}

public class ElementInfoToNameDisplayConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ElementBaseInfo elementInfo)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(elementInfo.ElementName))
        {
            return $"{elementInfo.ElementName} ({elementInfo.ElementTypeName})";
        }
        else
        {
            return elementInfo.ElementTypeName;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
