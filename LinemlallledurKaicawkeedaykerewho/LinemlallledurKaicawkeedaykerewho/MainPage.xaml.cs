using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LinemlallledurKaicawkeedaykerewho
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }
        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id.Contains("zhCN_KangkangM"));
                synthesizer.Voice = voice;

                var text = InputTextBox.Text;

                try
                {
                    SpeechSynthesisStream stream = await synthesizer.SynthesizeTextToStreamAsync(text);
                    using (stream)
                    {
                        FileSavePicker savePicker = new FileSavePicker();
                        savePicker.FileTypeChoices.Add("音频", new[] { ".wav" });

                        var result = await savePicker.PickSaveFileAsync();
                        var wordFile = result;

                        using (var wordFileStream = await wordFile.OpenStreamForWriteAsync())
                        {
                            await stream.AsStreamForRead().CopyToAsync(wordFileStream);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            }
        }
    }
}