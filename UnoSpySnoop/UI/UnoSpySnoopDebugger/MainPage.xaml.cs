using System.Collections.ObjectModel;
using System.Diagnostics;

using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Exceptions;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;
using dotnetCampus.Ipc.Threading;

using Microsoft.UI.Xaml.Data;
using UnoSpySnoopDebugger.Communications;
using UnoSpySnoopDebugger.IpcCommunicationContext;
using UnoSpySnoopDebugger.Models;
using UnoSpySnoopDebugger.View;

namespace UnoSpySnoopDebugger;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        var currentProcess = Process.GetCurrentProcess();
        var name = $"UnoSpySnoopDebugger_{currentProcess.ProcessName}_{currentProcess.Id}";
        var ipcProvider = new IpcProvider(name, new IpcConfiguration()
        {
            IpcTaskScheduling = IpcTaskScheduling.LocalOneByOne,
            IpcClientPipeConnector = new UnoSpySnoopDebuggerIpcClientPipeConnector(),
        });
        var jsonIpcDirectRoutedProvider = new JsonIpcDirectRoutedProvider(ipcProvider);
        IpcProvider = jsonIpcDirectRoutedProvider;

        _ = RefreshProcessInfoList();
    }

    public ObservableCollection<CandidateDebugProcessInfo> ProcessInfoList { get; } =
        new ObservableCollection<CandidateDebugProcessInfo>();

    private async Task RefreshProcessInfoList()
    {
        ProcessInfoList.Clear();

        var processes = Process.GetProcesses().ToList();

        await Parallel.ForEachAsync(processes, async (process, _) =>
        {
            var peerName = $"UnoSpySnoop_{process.ProcessName}_{process.Id}";

            try
            {
                JsonIpcDirectRoutedClientProxy client =
                    await IpcProvider.GetAndConnectClientAsync(peerName);
                var response = await client.GetResponseAsync<HelloResponse>(RoutedPathList.Hello);
                if (response is null)
                {
                    return;
                }

                if (response.SnoopVersionText != VersionInfo.VersionText)
                {
                    return;
                }

                var info = new CandidateDebugProcessInfo()
                {
                    Client = client,
                    CommandLine = response.CommandLine,
                    ProcessId = response.ProcessId.ToString(),
                    ProcessName = response.ProcessName,
                };

                DispatcherQueue.TryEnqueue(() =>
                {
                    ProcessInfoList.Add(info);
                });
            }
            catch (IpcClientPipeConnectionException e)
            {
                // Connection Fail
            }
        });
    }

    public JsonIpcDirectRoutedProvider IpcProvider { get; set; }

    private async void StartDebugButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProcessInfoListView.SelectedItem is CandidateDebugProcessInfo info)
        {
            if (info.Client is null)
            {
                return;
            }

#if !MACCATALYST
            Window.Current.Title = $"UnoSpySnoopDebugger - Debugging {info.ProcessName} PID:{info.ProcessId}";
#endif

            var snoopUserControl = new SnoopUserControl(info.Client);
            await snoopUserControl.StartAsync();

            RootGrid.Children.Clear();
            RootGrid.Children.Add(snoopUserControl);
        }
    }

    private void RefreshProcessInfoListButton_OnClick(object sender, RoutedEventArgs e)
    {
        _ = RefreshProcessInfoList();
    }
}

public class NotNullToIsEnableConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value is not null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
