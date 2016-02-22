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
using Windows.Web.Syndication;
using ViewModel;

namespace rss.ViewModel
{
    public class viewModel : notify_property
    {
        public viewModel()
        {
            //ce();
            //upload_file(null);
            syndication();
        }

        public ObservableCollection<rssstr> rsslist { set; get; } = new ObservableCollection<rssstr>();

        private void ce()
        {
            var url = "http://blog.csdn.net/lindexi_gd/article/details/50633565";
            var request = WebRequest.Create(url);

            request.Method = "GET";
            request.Headers["Cookie"] = "";
            request.BeginGetResponse(response_call_back, request);

            //Windows.Web.Syndication
        }

        public async void syndication()
        {
            var client = new SyndicationClient();

            //uri写在外面，为了在try之外不会说找不到变量
            Uri uri = null;

            //uri字符串
            var uriString = "http://www.win10.me/?feed=rss2";

            try
            {
                uri = new Uri(uriString);
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

                foreach (var item in feed.Items)
                {
                    displayCurrentItem(item);
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

        private void displayCurrentItem(SyndicationItem item)
        {
            var itemTitle = item.Title?.Text ?? "No title";
            var itemLink = item.Links == null ? "No link" : item.Links.FirstOrDefault().ToString();
            var itemContent = item.Content?.Text ?? "No content";
            var itemSummary = item.Summary.Text + "";
            reminder = itemTitle + "\n" + itemLink + "\n" + itemContent + "\n" + itemSummary + "\n";

            rsslist.Add(new rssstr(itemTitle, itemSummary));
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
            var UP_HOST = "http://up.qiniu.com";
            var url = UP_HOST;
            var request = WebRequest.Create(url);
            request.Method = "POST";
            request.BeginGetRequestStream(async result =>
            {
                var http = (HttpWebRequest) result.AsyncState;
                var buffer = new byte[1024];
                using (var stream = http.EndGetRequestStream(result))
                {
                    //using (Windows.Storage.StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                    //{
                    //    using (resource)
                    //    {

                    //    }
                    //}
                    using (var readStream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        using (var dataReader = new DataReader(readStream))
                        {
                            var size = readStream.Size;
                            if (size <= uint.MaxValue)
                            {
                                var numBytesLoaded = await dataReader.LoadAsync((uint) size);
                                buffer = new byte[size];
                                dataReader.ReadBytes(buffer);
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
            var http = (HttpWebRequest) result.AsyncState;
            var web_response = http.EndGetResponse(result);
            using (var stream = web_response.GetResponseStream())
            {
                using (var read = new StreamReader(stream))
                {
                    var content = read.ReadToEnd();
                    await
                        CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            () => { reminder = content; });
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