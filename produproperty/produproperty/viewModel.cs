using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace produproperty
{
    class viewModel : notify_property
    {
        public viewModel()
        {
            _m = new model();

            text = "选择要保存位置";
            writetext = true;
        }
        public bool firstget
        {
            set;
            get;
        }
        public bool updateproper
        {
            set;
            get;
        }
        public string text
        {
            set
            {
                _text = value;
                OnPropertyChanged();               
            }
            get
            {
                return _text;
            }
        }


        public void property()
        {
            text = _m.property(text, firstget, updateproper);
            //Random ran = new Random();
            //int n = 'z' - 'a' + 1;
            //List<char> str = new List<char>();
            //StringBuilder t = new StringBuilder();            
            //for (int i = 0; i < n; i++)
            //{
            //    str.Add((char)('a' + i));
            //}
            //for (int i = 0; i < n; i++)
            //{
            //    str.Add((char)('A' + i));
            //}
            //n = 10;
            //for (int i = 0; i < n; i++)
            //{
            //    str.Add((char)('0' + i));
            //}
            //n = 100000;
            //for (int i = 0; i < n; i++)
            //{
            //    t.Append(str[ran.Next() % str.Count]);
            //}
            //text = t.ToString();
        }

        /// <summary>
        /// 拖入图片
        /// </summary>
        public async void dropimg(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            if (writetext)
            {
                return;
            }

            var defer = e.GetDeferral();

            try
            {
                DataPackageView dataView = e.DataView;
                // 拖放类型为文件存储。
                if (dataView.Contains(StandardDataFormats.StorageItems))
                {
                    var files = await dataView.GetStorageItemsAsync();
                    StorageFile file = files.OfType<StorageFile>().First();
                    if (file.FileType == ".png" || file.FileType == ".jpg")
                    {
                        // 拖放的是图片文件。
                        BitmapImage bitmap = new BitmapImage();
                        await bitmap.SetSourceAsync(await file.OpenAsync(FileAccessMode.Read));
                        //ximg.ImageSource = bitmap;
                    }
                }
            }
            finally
            {
                defer.Complete();
            }
        }

        /// <summary>
        /// 获取存储文件位置
        /// </summary>
        public async void fileaddress()
        {
            //获取保存文件
            Windows.Storage.Pickers.FileSavePicker picker = new Windows.Storage.Pickers.FileSavePicker();
            //显示方式
            //picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            //选择最先的位置
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            //后缀名
            picker.SuggestedFileName = "博客";
            picker.DefaultFileExtension = ".md";
            picker.FileTypeChoices.Add("博客", new List<string>() { ".md",".txt" });

            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                reminder="选择 " + file.Name;
                _file = file;
                _folder = await file.GetParentAsync();
                fileaddresstext();
            }
        }



        public bool writetext
        {
            set
            {
                _writetext = value;
                OnPropertyChanged();
            }
            get
            {
                return _writetext;
            }
        }

        private string _text;
        private model _m;
        private StorageFolder _folder;
        private StorageFile _file;
        private bool _writetext;

        private void fileaddresstext()
        {
            writetext = false;
            text = "#" + _file.DisplayName + "#\r\n";
            
        }

        private async void imgfolder(StorageFile file)
        {
            string str = "image";
            StorageFolder image = await _folder.GetFolderAsync(str);
            if (image == null)
            {
                image = await _folder.CreateFolderAsync(str, CreationCollisionOption.OpenIfExists);
            }
            file=await file.CopyAsync(image, file.Name, NameCollisionOption.GenerateUniqueName);


        }
    }
}
