using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

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

    public Task StartAsync()
    {
        return RefreshAsync();
    }

    private async Task RefreshAsync()
    {

    }
}
