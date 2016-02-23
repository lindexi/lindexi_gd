// lindexi
// 16:26

#region

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Web.Syndication;
using ViewModel;

#endregion

namespace rss.ViewModel
{
    public class viewModel : notify_property
    {
        public viewModel()
        {
            //ce();
            //upload_file(null);
            syndication();

            rssVisibility = Visibility.Collapsed;
        }

        public ObservableCollection<rssstr> rsslist { set; get; } = new ObservableCollection<rssstr>();

        public Visibility rssVisibility
        {
            set
            {
                _rssVisibility = value == Visibility.Visible;
                OnPropertyChanged();
            }
            get
            {
                return _rssVisibility ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool _rssVisibility;

        private void ce()
        {
            string url = "http://blog.csdn.net/lindexi_gd/article/details/50633565";
            WebRequest request = WebRequest.Create(url);

            request.Method = "GET";
            request.Headers["Cookie"] = "";
            request.BeginGetResponse(response_call_back, request);

            //Windows.Web.Syndication
        }

        public async void syndication()
        {
            SyndicationClient client = new SyndicationClient();

            //uri写在外面，为了在try之外不会说找不到变量
            Uri uri = null;

            //uri字符串
            string uri_string = "http://www.win10.me/?feed=rss2";

            try
            {
                uri = new Uri(uri_string);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            try
            {
                //模拟http 
                // 如果没有设置可能出错
                client.SetRequestHeader("User-Agent",
                    "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

                SyndicationFeed feed = await client.RetrieveFeedAsync(uri);

                foreach (SyndicationItem item in feed.Items)
                {
                    display_current_item(item);
                }

                //foreach (var temp in rsslist)
                //{
                //    reminder = temp.summary;
                //}
            }
            catch
            {
                // Handle the exception here.
            }
        }

        private void display_current_item(SyndicationItem item)
        {
            string item_title = item.Title?.Text ?? "No title";
            string item_link = item.Links == null ? "No link" : item.Links.FirstOrDefault().ToString();
            string item_content = item.Content?.Text ?? "No content";
            string item_summary = item.Summary.Text + "";
            reminder = item_title + "\n" + item_link + "\n" + item_content + "\n" + item_summary + "\n";

            rsslist.Add(new rssstr(item_title, item_summary));
        }

        public async void upload_file(StorageFile file)
        {
            if (file == null)
            {
                file =
                    await
                        StorageFile.GetFileFromApplicationUriAsync(
                            new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));
            }
            string UP_HOST = "http://up.qiniu.com";
            string url = UP_HOST;
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            request.BeginGetRequestStream(async result =>
            {
                HttpWebRequest http = (HttpWebRequest) result.AsyncState;
                byte[] buffer = new byte[1024];
                using (Stream stream = http.EndGetRequestStream(result))
                {
                    //using (Windows.Storage.StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                    //{
                    //    using (resource)
                    //    {

                    //    }
                    //}
                    using (IRandomAccessStream read_stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        using (DataReader data_reader = new DataReader(read_stream))
                        {
                            ulong size = read_stream.Size;
                            if (size <= uint.MaxValue)
                            {
                                await data_reader.LoadAsync((uint) size);
                                buffer = new byte[size];
                                data_reader.ReadBytes(buffer);
                            }
                        }
                    }
                    stream.Write(buffer, 0, buffer.Length);

                    http.BeginGetResponse(response_call_back, http);
                }
            }, request);
        }


        private async void response_call_back(IAsyncResult result)
        {
            HttpWebRequest http = (HttpWebRequest) result.AsyncState;
            WebResponse web_response = http.EndGetResponse(result);
            using (Stream stream = web_response.GetResponseStream())
            {
                using (StreamReader read = new StreamReader(stream))
                {
                    string content = read.ReadToEnd();
                    await
                        CoreApplication.MainView.
                        CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () =>
                            {
                                reminder = content;
                            });
                }
            }
        }
    }


    /// <summary>
    /// </summary>
    public class rssstr
    {
        public rssstr(string title, string summary)
        {
            this.title = title;
            this.summary = WebUtility.HtmlDecode(Regex.Replace(summary, "<[^>]+?>", ""));
        }

        public string title { set; get; }
        public string summary { set; get; }
    }
}