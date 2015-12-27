using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace delayeddisplay
{
    public class viewModel : notify_property
    {
        public viewModel()
        {
            ce();
        }

        public void ce()
        {
            System.Net.Http.HttpClient http = new System.Net.Http.HttpClient();
            string str = "http://appserver.m.bing.net/BackgroundImageService/TodayImageService.svc/GetTodayImage?dateOffset=0&urlEncodeHeaders=true&osName=windowsPhone&osVersion=8.10&orientation=480x800&deviceName=WP8&mkt=en-US";

            Uri uri;
            uri = new Uri(str);
            student.img = new Windows.UI.Xaml.Media.Imaging.BitmapImage(uri);

            //System.Net.HttpWebRequest httpweb= (System.Net.HttpWebRequest)System.Net.WebRequest.Create(str);
            //httpweb.Method = "get";
            //httpweb.BeginGetRequestStream((result) =>
            //{
            //    System.Net.HttpWebResponse response;

            //    try
            //    {
            //        response = (System.Net.HttpWebResponse)httpweb.EndGetResponse(result);
            //    }
            //    catch 
            //    {

                    
            //    }

            //    if (httpweb.HaveResponse)
            //    {
            //        Uri uri;
            //        uri = new Uri(str);
            //        student.img = new Windows.UI.Xaml.Media.Imaging.BitmapImage(uri);
            //    }
            //}
            //, null);
        }

        public modelbusiness.student student
        {
            set
            {
                _student = value;
                OnPropertyChanged();
            }
            get
            {
                return _student;
            }
        }
        private modelbusiness.student _student=new modelbusiness.student()
        {
            name="学生",
            city="上海",
            age="100",
            //img=new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/gamersky_05small_10_20155131557102.jpg"))
        };
        private modelbusiness.model _model=new modelbusiness.model();
    }
}
