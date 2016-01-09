using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
namespace produproperty
{
    class model
    {
        public model(viewModel view)
        {
            _open = false;            
            this.view = view;
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

        public void openfile()
        {
            _open = true;
        }

        public void accessfolder(StorageFolder folder)
        {
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Clear();
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
        }

        public async void storage(StorageFolder folder)
        {
            string str = "image";
            if (!_open)
            {
                using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                {
                    using (DataWriter dataWriter = new DataWriter(transaction.Stream))
                    {
                        dataWriter.WriteString(text);
                        transaction.Stream.Size = await dataWriter.StoreAsync();
                        await transaction.CommitAsync();
                    }
                }

                file = await file.CopyAsync(folder, name + ".md", NameCollisionOption.GenerateUniqueName);

                StorageFolder image = await this.folder.GetFolderAsync(str);
                StorageFolder imagefolder = null;
                try
                {
                    imagefolder = await folder.GetFolderAsync(str);
                }
                catch
                {


                }

                if (imagefolder == null)
                {
                    imagefolder = await folder.CreateFolderAsync(str, CreationCollisionOption.OpenIfExists);
                }

                var filelist = await image.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.DefaultQuery);
                foreach (var t in filelist)
                {
                    await t.CopyAsync(imagefolder, t.Name, NameCollisionOption.GenerateUniqueName);
                }


                try
                {
                    Directory.Delete(image.Path, true);
                    foreach (var t in await this.folder.GetFilesAsync())
                    {
                        File.Delete(t.Path);
                    }
                }
                catch
                {

                }

                _open = true;
                this.folder = folder;
            }
            else
            {
                using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                {
                    using (DataWriter dataWriter = new DataWriter(transaction.Stream))
                    {
                        dataWriter.WriteString(text);
                        transaction.Stream.Size = await dataWriter.StoreAsync();
                        await transaction.CommitAsync();
                    }
                }
            }
        }

        public string property(string str, bool firstget, bool updateproper)
        {
            int i = 0;
            i = str.IndexOf("\r\n");
            while (i != -1)
            {
                stringproperty(str.Substring(0, i), firstget, updateproper);
                str = str.Substring(i + 2);
                i = str.IndexOf("\r\n");
            }

            stringproperty(str, firstget, updateproper);


            StringBuilder s = new StringBuilder();
            foreach (var temp in publicproperty)
            {
                s.Append(temp + "\r\n");
            }
            foreach (var temp in privatep)
            {
                s.Append(temp + "\r\n");
            }
            return s.ToString();
        }

        private List<string> publicproperty = new List<string>();
        private List<string> privatep = new List<string>();

        private StorageFile _file;
        private StorageFolder _folder;
        public bool _open;

        private viewModel view
        {
            set;
            get;
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


        private async void ce()
        {
            //默认存在
            string name = "请输入标题";

            //默认位置
            try
            {
                folder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Entries[0].Token);
            }
            catch
            {
                folder = ApplicationData.Current.LocalFolder;

            }

            try
            {
                file = await folder.GetFileAsync(name);
                using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
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
            catch
            {
                file = await folder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
                _text = file.Path;
            }

            

           

            _name = file.DisplayName;

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

        }

        public string _text;
        public string _name;

        private string stringproperty(string str, bool firstget, bool updateproper)
        {
            string a;
            string b;
            string c;
            string s;
            string g;
            a = string.Empty;
            b = string.Empty;
            c = string.Empty;
            s = string.Empty;
            g = string.Empty;

            stringproperty(str, ref a, ref b, ref c);

            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return string.Empty;
            }
            if (updateproper)
            {
                s = "            set\n            { \n                UpdateProper(ref _" + b + " , value);\n            }";
            }
            else
            {
                s = "            set\n            { \n                _" + b + " = value;\n                OnPropertyChanged(\"" + b + "\");\n            }";
            }


            g = "            get\n            {\n                return _" + b + ";\n            }";

            if (!firstget)
            {
                publicproperty.Add("        public " + a + " " + b + " \n        {\n" + s + "\n" + g + "\n        }");
            }
            else
            {
                publicproperty.Add("        public " + a + " " + b + " \n        {\n" + g + "\n" + s + "\n        }");
            }


            privatep.Add("        private " + a + " _" + b + " " + c + "");

            return a + b + c;
        }


        private string stringproperty(string str, ref string a, ref string b, ref string c)
        {
            a = null;
            b = null;
            c = null;
            if (string.IsNullOrWhiteSpace(str))
            {
                return string.Empty;
            }
            int i = 0;
            str = str.Trim();
            i = str.IndexOf(' ');
            if (i == -1)
            {
                return string.Empty;
            }
            a = str.Substring(0, i);
            str = str.Substring(i).Trim();
            i = str.IndexOf('=');
            if (i == -1)
            {
                b = str;
                return str;
            }
            b = str.Substring(0, i);
            str = str.Substring(i).Trim();
            c = str;
            if (string.Equals(c[c.Length - 1], ';'))
            {
                return str;
            }
            c += ';';
            return str;
        }
    }
}
