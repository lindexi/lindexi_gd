using Windows.Storage.Pickers;

namespace WichijarbejeHearlaridejenerewhu;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private async void UploadFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var fileOpenPicker = new FileOpenPicker();
        fileOpenPicker.FileTypeFilter.Add(".png");
        fileOpenPicker.FileTypeFilter.Add(".jpg");
        fileOpenPicker.FileTypeFilter.Add(".jpeg");

        var file = await fileOpenPicker.PickSingleFileAsync();
        await using var fileStream = await file.OpenStreamForReadAsync();

        var httpClient = new HttpClient();
        await httpClient.PostAsync("http://172.20.114.23:12779/", new StreamContent(fileStream));
    }
}
