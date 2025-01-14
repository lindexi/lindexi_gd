using WatchDog.Core.Context;
using WatchDog.Uno.WatchDogClient;

namespace WatchDog.Uno;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        var watchDogProvider = new WatchDogProvider("http://127.0.0.1:57725/");
        var feedDogResponse = await watchDogProvider.FeedAsync(new FeedDogInfo("测试应用", "运行"));
        if (feedDogResponse == null)
        {
            return;
        }

        var id = feedDogResponse.FeedDogResult.Id;
        var currentDogId = Guid.NewGuid().ToString("N");

        var wangResponse = await watchDogProvider.GetWangAsync(new GetWangInfo(currentDogId));
        if (wangResponse == null)
        {
            return;
        }

        foreach (var wangInfo in wangResponse.GetWangResult.WangList)
        {
            
        }
    }
}
