using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using WatchDog.Core.Context;
using WatchDog.Service.Contexts;
using WatchDog.Uno.ViewModels;
using WatchDog.Uno.WatchDogClient;

namespace WatchDog.Uno;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    public FeedDogViewModel CurrentFeedDogViewModel { get; } = new FeedDogViewModel();
    public WatchDogViewModel WatchDogViewModel => (WatchDogViewModel) DataContext;

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        FeedDogButton.Focus(FocusState.Programmatic);
        _ = WatchDogViewModel.WatchAsync();

        WatchDogViewModel.NotifyWang += (o, info) =>
        {
            var lastFeedDogInfo = info.FeedDogInfo;

            var feedDogInfo = lastFeedDogInfo.FeedDogInfo;
            var offset = DateTimeOffset.Now - lastFeedDogInfo.LastUpdateTime;
            var message = $"名为 '{feedDogInfo.Name}', Id为 '{lastFeedDogInfo.Id}'\r\n已经超过 {offset.TotalSeconds}秒没有更新。\r\n最后更新状态是 {feedDogInfo.Status}";
            var escaped = new System.Xml.Linq.XText(message).ToString();

            var xmlDocument = new XmlDocument();
            // lang=xml
            var toast = $"""
                        <toast>
                            <visual>
                                <binding template='ToastText01'>
                                    <text id="1">{escaped}</text>
                                </binding>
                            </visual>
                        </toast>
                        """;
            xmlDocument.LoadXml(xml: toast);

#if WINDOWS10_0_19041_0_OR_GREATER
            var toastNotification = new ToastNotification(xmlDocument);
            var toastNotificationManagerForUser = ToastNotificationManager.GetDefault();
            var toastNotifier = toastNotificationManagerForUser.CreateToastNotifier(applicationId: "WatchDog");
            toastNotifier.Show(toastNotification);
#endif
        };
        return;

        //var watchDogProvider = new WatchDogProvider("http://127.0.0.1:57725/");
        //var feedDogResponse = await watchDogProvider.FeedAsync(new FeedDogInfo("测试应用", "运行"));
        //if (feedDogResponse == null)
        //{
        //    return;
        //}

        //var id = feedDogResponse.FeedDogResult.Id;
        //var currentDogId = Guid.NewGuid().ToString("N");

        //var wangResponse = await watchDogProvider.GetWangAsync(new GetWangInfo(currentDogId));
        //if (wangResponse == null)
        //{
        //    return;
        //}

        //foreach (var wangInfo in wangResponse.GetWangResult.WangList)
        //{

        //}
    }

    private async void FeedDogButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var watchDogProvider = WatchDogViewModel.WatchDogProvider;

            // 通知间隔时间
            uint notifyIntervalSecond = 60 * 30;
            uint delaySecond = 60;
            // 如果经过了多久都没有响应，则清除喂狗信息。默认是 7 天
            int maxCleanTimeSecond = 60 * 60 * 24 * 7;
#if DEBUG
            notifyIntervalSecond = 3;
            delaySecond = 3;
            maxCleanTimeSecond = 60 * 3;// 调试下 3 分钟就好，不要影响正式环境
#endif
            var feedDogInfo = new FeedDogInfo(CurrentFeedDogViewModel.Name, CurrentFeedDogViewModel.Status,
                CurrentFeedDogViewModel.Id, DelaySecond: delaySecond, NotifyIntervalSecond: notifyIntervalSecond,
                MaxCleanTimeSecond: maxCleanTimeSecond);
            FeedDogResponse? feedDogResponse = await watchDogProvider.FeedAsync(feedDogInfo);
            if (feedDogResponse == null)
            {
                FeedDogResultTextBox.Text = $"{DateTime.Now:HH:mm:ss,fff} 喂狗失败";
                return;
            }

            var feedDogResult = feedDogResponse.FeedDogResult;
            CurrentFeedDogViewModel.Id = feedDogResult.Id;
            FeedDogResultTextBox.Text = $"{DateTime.Now:HH:mm:ss,fff} 喂狗成功 {feedDogResult}";
        }
        catch (Exception exception)
        {
            FeedDogResultTextBox.Text = $"{DateTime.Now:HH:mm:ss,fff} 喂狗失败 {exception}";
        }
    }

    private void MuteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button) sender;
        var wangModel = (WangModel) button.DataContext;
        WatchDogViewModel.Mute(wangModel);
    }

    private void PerpetualMuteButton_OnClick(object sender, RoutedEventArgs e)
    {
        // 永久静默
        var button = (Button) sender;
        var wangModel = (WangModel) button.DataContext;
        WatchDogViewModel.PerpetualMute(wangModel);
    }
}
