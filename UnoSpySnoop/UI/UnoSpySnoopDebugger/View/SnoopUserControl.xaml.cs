using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
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
        get { return (List<ElementProxy>)GetValue(CurrentElementTreeProperty); }
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
