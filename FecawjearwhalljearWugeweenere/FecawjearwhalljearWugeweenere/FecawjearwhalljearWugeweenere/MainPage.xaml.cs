using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.FilePicker;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FecawjearwhalljearWugeweenere
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_OnClicked(object sender, EventArgs e)
        {
            var pickFile = await CrossFilePicker.Current.PickFile();
            if (pickFile is null)
            {
                // 用户拒绝选择文件
            }
            else
            {
                //var shareFile = new ShareFile();
                FileText.Text = $@"选取文件路径 :{pickFile.FilePath}";
            }
        }
    }
}
