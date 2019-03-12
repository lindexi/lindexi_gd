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
            img = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/gamersky_05small_10_20155131557102.jpg"));
            ce();
        }
        public Windows.UI.Xaml.Media.ImageSource img
        {
            set
            {
                _img = value;
                OnPropertyChanged();
            }
            get
            {
                return _img;
            }
        }
        private Windows.UI.Xaml.Media.ImageSource _img;

        public async void ce()
        {
            Windows.Storage.Pickers.FileOpenPicker pick;
            pick = new Windows.Storage.Pickers.FileOpenPicker();
            pick.FileTypeFilter.Add(".jpg");
            Windows.Storage.StorageFile file = await pick.PickSingleFileAsync();
            if (file != null)
            {
                Windows.UI.Xaml.Media.Imaging.BitmapImage temp = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                await temp.SetSourceAsync(await file.OpenAsync(Windows.Storage.FileAccessMode.Read));
                img = temp;
            }
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
        private modelbusiness.student _student = new modelbusiness.student()
        {
            name = "学生",
            city = "上海",
            age = "100",
            //img=new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/gamersky_05small_10_20155131557102.jpg"))
        };
        private modelbusiness.model _model = new modelbusiness.model();
    }
}
