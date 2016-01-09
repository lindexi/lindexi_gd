using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace produproperty
{
    class viewModel : notify_property
    {
        public viewModel()
        {
            _m = new model(this);

            //text = "选择要保存位置";
            writetext = false;

            ce();

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
                _m._text = value;
                OnPropertyChanged();
            }
            get
            {
                return _m._text;
            }
        }

        public Action<int, int> selectchange
        {
            set;
            get;
        }

        public int select
        {
            set
            {
                _select = value;
            }
            get
            {
                return _select;
            }
        }

        public string name
        {
            set
            {
                _m._name = value;
                OnPropertyChanged();
            }
            get
            {
                return _m._name;
            }
        }

        public async void clipboard(Windows.UI.Xaml.Controls.TextControlPasteEventArgs e)
        {
            if (writetext)
            {
                return;
            }
            e.Handled = true;
            DataPackageView con = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            string str = string.Empty;
            //文本
            if (con.Contains(StandardDataFormats.Text))
            {
                str = await con.GetTextAsync();
                tianjiatext(str);
            }

            //图片
            if (con.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference img = await con.GetBitmapAsync();
                var imgstream = await img.OpenReadAsync();
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(imgstream);

                WriteableBitmap src = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                src.SetSource(imgstream);

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imgstream);
                PixelDataProvider pxprd = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, new BitmapTransform(), ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
                byte[] buffer = pxprd.DetachPixelData();

                str = "image";
                StorageFolder folder = await _folder.GetFolderAsync(str);

                StorageFile file = await folder.CreateFileAsync(DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + ".png", CreationCollisionOption.GenerateUniqueName);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, decoder.PixelWidth, decoder.PixelHeight, decoder.DpiX, decoder.DpiY, buffer);
                    await encoder.FlushAsync();

                    str = $"![这里写图片描述](image/{file.Name})";

                    tianjiatext(str);
                    //text = text.Insert(select, str);

                    //selectchange(select + 2, 7);

                    //using (StreamWriter s=new StreamWriter(fileStream as Stream))
                    //{
                    //    s.Write(img);
                    //    s.Flush();
                    //}
                }
                //{
                //    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                //    encoder.SetPixelData(
                //        BitmapPixelFormat.Bgra8,
                //        BitmapAlphaMode.Ignore,
                //        (uint)src.PixelWidth,
                //        (uint)src.PixelHeight,
                //        Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi,
                //        Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi,
                //        src.PixelBuffer.ToArray());
                //    await encoder.FlushAsync();
                //}


                //using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                //{
                //    using (DataWriter dataWriter = new DataWriter(transaction.Stream))
                //    {
                //        dataWriter.WriteBuffer(src.PixelBuffer);
                //        await transaction.CommitAsync();
                //    }
                //}

                //IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

                //BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, fileStream);


                //Stream stream = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsStream(src.PixelBuffer);

                //byte[] pixels = new byte[src.PixelBuffer.Length];
                //stream.Read(pixels,0, pixels.Length);
                ////pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(src.PixelBuffer, 0, (int)src.PixelBuffer.Length);

                //////pixal format shouldconvert to rgba8

                //for (int i = 0; i < pixels.Length; i += 4)

                //{

                //    byte temp = pixels[i];

                //    pixels[i] = pixels[i + 2];

                //    pixels[i + 2] = temp;

                //}

                //encoder.SetPixelData(

                // BitmapPixelFormat.Rgba8,

                // BitmapAlphaMode.Straight,

                // (uint)src.PixelWidth,

                // (uint)src.PixelHeight,

                // 96, // Horizontal DPI

                // 96, // Vertical DPI

                // pixels

                // );

                //await encoder.FlushAsync();

                //str = $"![这里写图片描述](image/{file.Name})";
                //text = text.Insert(select, str);

                //selectchange(select + 2, 7);

            }

            //文件
            if (con.Contains(StandardDataFormats.StorageItems))
            {
                var filelist = await con.GetStorageItemsAsync();
                StorageFile file = filelist.OfType<StorageFile>().First();
                imgfolder(file);
            }


            //imageclipboard(con);
            //textclipboard(con);
            //storageclipboard(con);
            //var image = await con.GetBitmapAsync();


            //StorageFile file = await _folder.CreateFileAsync("adxbcyue.jpg", CreationCollisionOption.GenerateUniqueName);
            //using (IRandomAccessStream readStream = await image.OpenReadAsync())
            //{
            //    using (DataReader dataReader = new DataReader(readStream))
            //    {
            //        UInt64 size = readStream.Size;
            //        if (size <= UInt32.MaxValue)
            //        {
            //            UInt32 numBytesLoaded = await dataReader.LoadAsync((UInt32)size);
            //            string fileContent = dataReader.ReadString(numBytesLoaded);
            //            using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
            //            {

            //                using (DataWriter dataWriter = new DataWriter(transaction.Stream))
            //                {
            //                    dataWriter.WriteString(fileContent);
            //                    transaction.Stream.Size = await dataWriter.StoreAsync();
            //                    await transaction.CommitAsync();
            //                }
            //            }
            //        }
            //    }
            //}            
        }

        public async void accessfolder()
        {
            FolderPicker pick = new FolderPicker();
            pick.FileTypeFilter.Add("*");
            StorageFolder folder = await pick.PickSingleFolderAsync();
            if (folder != null)
            {
                _m.accessfolder(folder);
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
                    //if (file.FileType == ".png" || file.FileType == ".jpg")
                    //{
                    // 拖放的是图片文件。
                    //BitmapImage bitmap = new BitmapImage();
                    //await bitmap.SetSourceAsync(await file.OpenAsync(FileAccessMode.Read));
                    //ximg.ImageSource = bitmap;
                    imgfolder(file);
                    //}
                }
            }
            finally
            {
                defer.Complete();
            }
        }

        public async void folderaddress()
        {
            Windows.Storage.Pickers.FolderPicker picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
               Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".md");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                _folder = folder;
                folderaddresstext();
            }
        }

        public string addressfolder
        {
            set
            {

            }
            get
            {
                return _folder.Path;
            }
        }


        public async Task file_open()
        {
            Windows.Storage.Pickers.FileOpenPicker picker = new Windows.Storage.Pickers.FileOpenPicker();
            //显示方式
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            //选择最先的位置
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            //后缀名
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".md");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                reminder = "选择 " + file.Name;
                _file = file;
                fileaddresstext();
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
            picker.FileTypeChoices.Add("博客", new List<string>() { ".md", ".txt" });


            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                reminder = "选择 " + file.Name;
                _file = file;
                _folder = await file.GetParentAsync();
                fileaddresstext();
            }

        }
        /// <summary>
        /// 保存文件
        /// </summary>
        public async void storage()
        {
            if (_m._open)
            {
                _m.storage(_folder);
                return;
            }

            Windows.Storage.Pickers.FolderPicker picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
               Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".md");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                _m.storage(folder);
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


        private model _m;
        private StorageFolder _folder
        {
            set
            {
                _m.folder = value;
            }
            get
            {
                return _m.folder;
            }
        }
        private StorageFile _file
        {
            set
            {
                _m.file = value;
            }
            get
            {
                return _m.file;
            }
        }
        private bool _writetext;
        private int _select;



        private async void fileaddresstext()
        {
            writetext = false;
            //text = "#" + _file.DisplayName + "#\r\n";

            using (IRandomAccessStream readStream = await _file.OpenAsync(FileAccessMode.Read))
            {
                using (DataReader dataReader = new DataReader(readStream))
                {
                    UInt64 size = readStream.Size;
                    if (size <= UInt32.MaxValue)
                    {
                        UInt32 numBytesLoaded = await dataReader.LoadAsync((UInt32)size);
                        text = dataReader.ReadString(numBytesLoaded);
                    }
                }
            }

            reminder = "打开" + _file.Path;

            if (_folder == null)
            {
                reminder = "没有找到保存文件夹";
                folderaddress();
            }
        }

        private void folderaddresstext()
        {
            //writetext = false;
            //string str = "输入标题";
            //text = "#" + str + "#\r\n";
            //selectchange(1, str.Length);        
        }

        private async void imgfolder(StorageFile file)
        {
            string str = "image";
            StorageFolder image = null;
            try
            {
                image = await _folder.GetFolderAsync(str);
            }
            catch
            {


            }
            if (image == null)
            {
                image = await _folder.CreateFolderAsync(str, CreationCollisionOption.OpenIfExists);
            }
            file = await file.CopyAsync(image, file.Name, NameCollisionOption.GenerateUniqueName);

            if (file.FileType == ".png" || file.FileType == ".jpg")
            {
                str = $"![这里写图片描述](image/{file.Name})";
                tianjiatext(str);
                //text = text.Insert(select, str);

                //selectchange(select + 2, 7);
            }
            else
            {
                str = $"[{file.Name}](image/{file.Name})";
                tianjiatext(str);

                //text = text.Insert(select, str);

                //selectchange(select + str.Length+1,0);
            }
        }

        private void tianjiatext(string str)
        {
            int n;
            n = select;
            int i;
            for (i = 0; n > 0 && i < text.Length; i++)
            {
                if (text[i] != '\r' && text[i] != '\n')
                {
                    n--;
                }
            }
            text = text.Insert(i, str);
            selectchange(select + str.Length + 1, 0);
        }

        private void ce()
        {

        }

        //private void imageclipboard(DataPackageView con)
        //{

        //}
        //private void storageclipboard(DataPackageView con)
        //{

        //}

        //private async void textclipboard(DataPackageView con)
        //{
        //    try
        //    {
        //        var file = await con.GetStorageItemsAsync();
        //        foreach (var t in file)
        //        {
        //            if (t.IsOfType(StorageItemTypes.File))
        //            {
        //                StorageFile f = t as StorageFile;
        //            }
        //        }
        //    }
        //    catch
        //    {


        //    }
        //}
    }
}
