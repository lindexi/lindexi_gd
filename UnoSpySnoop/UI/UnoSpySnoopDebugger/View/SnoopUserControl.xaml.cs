using System.CodeDom.Compiler;

using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Data;

using UnoSpySnoopDebugger.IpcCommunicationContext;

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

    public Task StartAsync()
    {
        return RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var elementProxy = await Client.GetResponseAsync<ElementProxy>(RoutedPathList.GetRootVisualTree);
        _rootElement = elementProxy!;
        CurrentElementTree = new List<ElementProxy>(1) { _rootElement };
    }
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
