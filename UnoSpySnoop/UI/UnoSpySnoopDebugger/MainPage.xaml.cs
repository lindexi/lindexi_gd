using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Exceptions;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;
using dotnetCampus.Ipc.Pipes.PipeConnectors;
using dotnetCampus.Ipc.Threading;
using dotnetCampus.Ipc.Utils.Logging;

using Microsoft.UI.Xaml.Data;

using UnoSpySnoopDebugger.IpcCommunicationContext;
using UnoSpySnoopDebugger.View;

namespace UnoSpySnoopDebugger;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        //Loaded += MainPage_Loaded;
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

        var dependencyPropertyInfoList = new List<DependencyPropertyInfo>();
        GetStaticDependencyProperty(this, typeof(SnoopUserControl), dependencyPropertyInfoList);
    }

    public ObservableCollection<CandidateDebugProcessInfo> ProcessInfoList { get; } =
        new ObservableCollection<CandidateDebugProcessInfo>();

    private async Task RefreshProcessInfoList()
    {
        ProcessInfoList.Clear();

        var processes = Process.GetProcesses().ToList();

        var list = new CandidateDebugProcessInfo?[processes.Count];

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
                    // 版本不对
                }

                var index = processes.IndexOf(process);
                list[index] = new CandidateDebugProcessInfo()
                {
                    Client = client,
                    CommandLine = response.CommandLine,
                    ProcessId = response.ProcessId.ToString(),
                    ProcessName = response.ProcessName,
                };
            }
            catch (IpcClientPipeConnectionException e)
            {
                // Connection Fail
            }
        });

        foreach (var info in list)
        {
            if (info?.Client != null)
            {
                ProcessInfoList.Add(info);
            }
        }
    }

    record DependencyPropertyInfo(string Name, string Value, string DeclaringTypeFullName);

    private void GetStaticDependencyProperty(DependencyObject obj, Type type, List<DependencyPropertyInfo> infoList)
    {
        foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
        {
            if (fieldInfo.FieldType == typeof(DependencyProperty))
            {
                if (fieldInfo.GetValue(null) is DependencyProperty dependencyProperty)
                {
                    AddToInfoList(dependencyProperty);
                }
            }
        }

        foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Static | BindingFlags.Public))
        {
            if (propertyInfo.PropertyType == typeof(DependencyProperty))
            {
                if (propertyInfo.GetValue(null) is DependencyProperty dependencyProperty)
                {
                    AddToInfoList(dependencyProperty);
                }
            }
        }

        if (type.BaseType is { } baseType)
        {
            GetStaticDependencyProperty(obj, baseType, infoList);
        }

        void AddToInfoList(DependencyProperty dependencyProperty)
        {
            try
            {
                PropertyInfo nameProperty =
                    typeof(DependencyProperty).GetProperty("Name", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var name = (string) nameProperty.GetValue(dependencyProperty)!;

                var value = obj.GetValue(dependencyProperty);
                var valueText = value?.ToString() ?? "_<NULL>";

                var info = new DependencyPropertyInfo(name, valueText, type.FullName!);
                infoList.Add(info);
            }
            catch (Exception e)
            {
                if (e is System.InvalidOperationException)
                {
                    // 如获取属性不在相同类型
                }
            }
        }
    }

    public JsonIpcDirectRoutedProvider IpcProvider { get; set; }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        var debugName = "UnoSpySnoop"; // todo Update to selected name
        JsonIpcDirectRoutedClientProxy client = await IpcProvider.GetAndConnectClientAsync(debugName);
        var snoopUserControl = new SnoopUserControl(client);
        await snoopUserControl.StartAsync();

        RootGrid.Children.Add(snoopUserControl);
    }

    private async void StartDebugButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProcessInfoListView.SelectedItem is CandidateDebugProcessInfo info)
        {
            if (info.Client is null)
            {
                return;
            }

            var snoopUserControl = new SnoopUserControl(info.Client);
            await snoopUserControl.StartAsync();

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

public class CandidateDebugProcessInfo
{
    public string? ProcessName { get; set; }
    public string? ProcessId { get; set; }
    public string? CommandLine { get; set; }
    public JsonIpcDirectRoutedClientProxy? Client { get; set; }
}

class UnoSpySnoopDebuggerIpcClientPipeConnector : IIpcClientPipeConnector
{
    public async Task<IpcClientNamedPipeConnectResult> ConnectNamedPipeAsync(IpcClientPipeConnectionContext ipcClientPipeConnectionContext)
    {
        try
        {
            // With special timeout
            await ipcClientPipeConnectionContext.NamedPipeClientStream.ConnectAsync(TimeSpan.FromSeconds(1),
                ipcClientPipeConnectionContext.CancellationToken);
        }
        catch (TimeoutException e)
        {
            return new IpcClientNamedPipeConnectResult(false, e.Message);
        }

        return new IpcClientNamedPipeConnectResult(true);
    }
}
