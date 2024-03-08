using System.Reflection;

using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

using UnoSpySnoopDebugger.View;

namespace UnoSpySnoopDebugger;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;

        var ipcProvider = new JsonIpcDirectRoutedProvider();
        IpcProvider = ipcProvider;

        var dependencyPropertyInfoList = new List<DependencyPropertyInfo>();
        GetStaticDependencyProperty(this, typeof(SnoopUserControl), dependencyPropertyInfoList);
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
                var name = (string)nameProperty.GetValue(dependencyProperty)!;

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
}
