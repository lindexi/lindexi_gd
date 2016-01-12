using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace produproperty
{
    class model
    {
        public model(viewModel view)
        {
            this.view = view;
            ran = new Random();
            ce();
        }

        public StorageFile file
        {
            set
            {
                _file = value;
            }
            get
            {
                return _file;
            }
        }

        public StorageFolder folder
        {
            set
            {
                _folder = value;
            }
            get
            {
                return _folder;
            }
        }



        private string text
        {
            set
            {
                if (view == null)
                {
                    _text = value;
                }
                else
                {
                    view.text = value;
                }
            }
            get
            {
                if (view == null)
                {
                    return _text;
                }
                else
                {
                    return view.text;
                }
            }
        }

        private bool writetext
        {
            set
            {
                _writetext = value;
            }
            get
            {
                return _writetext;
            }
        }

        private string name
        {
            set
            {
                if (view == null)
                {
                    _name = value;
                }
                else
                {
                    view.name = value;
                }
            }
            get
            {
                if (view == null)
                {
                    return _name;
                }
                else
                {
                    return view.name;
                }
            }
        }

        public async Task<string> clipboard(DataPackageView con)
        {
            string str = string.Empty;
            //文本
            if (con.Contains(StandardDataFormats.Text))
            {
                str = await con.GetTextAsync();
                //tianjiatext(str);
                return str;
            }

            //图片
            if (con.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference img = await con.GetBitmapAsync();
                var imgstream = await img.OpenReadAsync();
                Windows.UI.Xaml.Media.Imaging.BitmapImage bitmap = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                bitmap.SetSource(imgstream);

                Windows.UI.Xaml.Media.Imaging.WriteableBitmap src = new Windows.UI.Xaml.Media.Imaging.WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                src.SetSource(imgstream);

                Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(imgstream);
                Windows.Graphics.Imaging.PixelDataProvider pxprd = await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.RespectExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
                byte[] buffer = pxprd.DetachPixelData();

                str = "image";
                StorageFolder folder = await _folder.GetFolderAsync(str);

                StorageFile file = await folder.CreateFileAsync(DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() +(ran.Next()%10000).ToString()+ ".png", CreationCollisionOption.GenerateUniqueName);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, decoder.PixelWidth, decoder.PixelHeight, decoder.DpiX, decoder.DpiY, buffer);
                    await encoder.FlushAsync();

                    str = $"![这里写图片描述](image/{file.Name})\r\n\r\n";
                }
            }

            //文件
            if (con.Contains(StandardDataFormats.StorageItems))
            {
                var filelist = await con.GetStorageItemsAsync();
                StorageFile file = filelist.OfType<StorageFile>().First();
                return await imgfolder(file);
            }

            return str;
        }

        public async void open_file(StorageFile file)
        {
            _open = true;
            await storage();
            this.file = file;

            using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
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
            name = file.DisplayName;
            reminder = "打开" + file.Path;
        }

        public async Task<string> imgfolder(StorageFile file)
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
                str = $"![这里写图片描述](image/{file.Name})\r\n\r\n";
                return str;
            }
            else
            {
                str = $"[{file.Name}](image/{file.Name})\r\n\r\n";
                return str;
            }
        }

        public async void accessfolder(StorageFolder folder)
        {
            if (string.Equals(this.folder.Path, folder.Path))
            {
                return;
            }

            writetext = false;

            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Clear();
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);

            //image 文件夹
            string str = "image";
            StorageFolder image = null;
            try
            {
                image = await folder.GetFolderAsync(str);
            }
            catch
            {


            }
            if (image == null)
            {
                image = await folder.CreateFolderAsync(str, CreationCollisionOption.OpenIfExists);
            }
            if (!this.folder.Path.Equals(ApplicationData.Current.LocalFolder.Path))
            {
                await storage();
            }
            this.folder = folder;
        }

        public async Task storage()
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
            {
                using (DataWriter dataWriter = new DataWriter(transaction.Stream))
                {
                    dataWriter.WriteString(text);
                    transaction.Stream.Size = await dataWriter.StoreAsync();
                    await transaction.CommitAsync();
                }
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "请输入标题";
                return;
            }
            if (!_open)
            {
                file = await file.CopyAsync(folder, name + ".md", NameCollisionOption.GenerateUniqueName);
                try
                {
                    foreach (var t in await ApplicationData.Current.LocalFolder.GetFilesAsync())
                    {
                        System.IO.File.Delete(t.Path);
                    }
                }
                catch
                {

                }
                _open = true;
            }

            reminder = reminder = "保存文件" + file.Path + " " + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();
        }



        public string _text;
        public string _name;
        private viewModel view;
        private StorageFile _file;
        private StorageFolder _folder;
        private bool _open;
        public bool _writetext;
        private string reminder
        {
            set
            {
                view.reminder = value;
            }
            get
            {
                return view.reminder;
            }
        }
        private Random ran
        {
            set;
            get;
        }
        private async void ce()//2016年1月10日11:04:29
        {
            _open = false;
            string str;
            //默认位置
            try
            {
                folder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Entries[0].Token);
                writetext = false;
            }
            catch
            {
                folder = ApplicationData.Current.LocalFolder;
                //没有默认位置

                writetext = true;
                _text = "请选择默认保存位置";
            }

            //image 文件夹
            str = "image";
            StorageFolder image = null;
            try
            {
                image = await folder.GetFolderAsync(str);
            }
            catch
            {


            }
            if (image == null)
            {
                image = await folder.CreateFolderAsync(str, CreationCollisionOption.OpenIfExists);
            }

            //没有上次保存
            str = "请输入标题";
            bool open = false;
            try
            {
                file = (await ApplicationData.Current.LocalFolder.GetFilesAsync()).First<StorageFile>();
                open = true;
            }
            catch
            {
                //新建
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync(str, CreationCollisionOption.GenerateUniqueName);
                open = false;
            }

            if (open)
            {
                using (IRandomAccessStream readStream = await _file.OpenAsync(FileAccessMode.Read))
                {
                    using (DataReader dataReader = new DataReader(readStream))
                    {
                        UInt64 size = readStream.Size;
                        if (size <= UInt32.MaxValue)
                        {
                            UInt32 numBytesLoaded = await dataReader.LoadAsync((UInt32)size);
                            _text = dataReader.ReadString(numBytesLoaded);
                        }
                    }
                }
            }
            else
            {
                _text = @"
拖入图片自动生成![](图片)，粘贴自动生成![](图片)
按ctrl+k快速输入代码";
            }
            _name = file.DisplayName;
        }
    }
}
