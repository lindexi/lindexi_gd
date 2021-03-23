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

        private static async Task ConvertWordListToFile(IList<string> wordList)
        {
            var wordFolder =
                await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
                    DateTime.Now.ToString("yyyy MM dd hhmmss"));
            var wordFileInfoList = new List<WordFileInfo>();

            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                foreach (var word in wordList)
                {
                    try
                    {
                        SpeechSynthesisStream stream = await synthesizer.SynthesizeTextToStreamAsync(word);
                        using (stream)
                        {
                            var wordFile =
                                await wordFolder.CreateFileAsync($"{word}.wav",
                                    CreationCollisionOption.GenerateUniqueName);

                            using (var wordFileStream = await wordFile.OpenStreamForWriteAsync())
                            {
                                await stream.AsStreamForRead().CopyToAsync(wordFileStream);
                            }

                            wordFileInfoList.Add(new WordFileInfo(word, wordFile.Name));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }

            var wordListFile = await wordFolder.CreateFileAsync("word list.txt");

            await FileIO.WriteLinesAsync(wordListFile, wordFileInfoList.Select(temp => $"{temp.Word}|{temp.FileName}"));

            await Launcher.LaunchFolderAsync(ApplicationData.Current.TemporaryFolder);
        }

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            // 词典通过 ChobigarqerKuhawnakayewe 创建
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add(".txt");
            var wordFile = await fileOpenPicker.PickSingleFileAsync();
            var wordList = await FileIO.ReadLinesAsync(wordFile);
            await ConvertWordListToFile(wordList);
        }
    }
}