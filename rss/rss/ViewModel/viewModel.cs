using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

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
            string url = "http://blog.csdn.net/lindexi_gd/article/details/50633565";
            WebRequest request = HttpWebRequest.Create(url);

            request.Method = "GET";
            request.Headers["Cookie"] = "";
            request.BeginGetResponse(response_call_back, request);

            //Windows.Web.Syndication
        }

        public async void syndication()
        {
            Windows.Web.Syndication.SyndicationClient client = new Windows.Web.Syndication.SyndicationClient();
            Windows.Web.Syndication.SyndicationFeed feed;

            // The URI is validated by catching exceptions thrown by the Uri constructor.
            //uri写在外面，为了在try之外不会说找不到变量
            Uri uri = null;

            //uri字符串
            string uriString = "http://www.win10.me/?feed=rss2";

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
                client.SetRequestHeader("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

                feed = await client.RetrieveFeedAsync(uri);

                foreach (Windows.Web.Syndication.SyndicationItem item in feed.Items)
                {
                    displayCurrentItem(item);
                }

                //foreach (var temp in rsslist)
                //{
                //    reminder = temp.summary;
                //}
            }
            catch (Exception ex)
            {
                // Handle the exception here.
            }
        }

        private void displayCurrentItem(Windows.Web.Syndication.SyndicationItem item)
        {
            string itemTitle = item.Title == null ? "No title" : item.Title.Text;
            string itemLink = item.Links == null ? "No link" : item.Links.FirstOrDefault().ToString();
            string itemContent = item.Content == null ? "No content" : item.Content.Text;
            string itemSummary = item.Summary.Text + "";
            reminder = itemTitle + "\n" + itemLink + "\n" + itemContent+"\n"+itemSummary+"\n";

            rsslist.Add(new rssstr(itemTitle, itemSummary));
            
            
        }

        public async void upload_file(Windows.Storage.StorageFile file)
        {
            if (file == null)
            {
                 file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));

            }
            string UP_HOST = "http://up.qiniu.com";
            var url = UP_HOST;
            var request = HttpWebRequest.Create(url);
            request.Method = "POST";
            request.BeginGetRequestStream(async result =>
            {
                HttpWebRequest http = (HttpWebRequest)result.AsyncState;
                byte[] buffer = new byte[1024];
                using (Stream stream = http.EndGetRequestStream(result))
                {
                    //using (Windows.Storage.StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                    //{
                    //    using (resource)
                    //    {

                    //    }
                    //}
                    using (IRandomAccessStream readStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        using (DataReader dataReader = new DataReader(readStream))
                        {
                            UInt64 size = readStream.Size;
                            if (size <= UInt32.MaxValue)
                            {
                                UInt32 numBytesLoaded = await dataReader.LoadAsync((UInt32)size);
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
            HttpWebRequest http = (HttpWebRequest)result.AsyncState;
            WebResponse web_response = http.EndGetResponse(result);
            using (Stream stream = web_response.GetResponseStream())
            {
                using (StreamReader read = new StreamReader(stream))
                {
                    string content = read.ReadToEnd();
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        reminder = content;
                    });
                }
            }
        }


    }


    /// <summary>
    ///
    /// </summary>
    public class rssstr
    {
        public rssstr(string title,string summary)
        {
            this.title = title;
            this.summary = WebUtility.HtmlDecode(Regex.Replace(summary, "<[^>]+?>", ""));
        }
        
        public string title { set; get; }
        public string summary { set; get; }
    }

}
