using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
using Windows.Storage.Streams;

namespace rss.ViewModel
{
    public class viewModel : notify_property
    {
        public viewModel()
        {
            //ce();
            upload_file(null);
        }
        private void ce()
        {
            string url = "http://blog.csdn.net/lindexi_gd/article/details/50633565";
            WebRequest request = HttpWebRequest.Create(url);

            request.Method = "GET";
            request.Headers["Cookie"] = "";
            request.BeginGetResponse(response_call_back, request);
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
}
