using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using ViewModel;
using Windows.Storage.Pickers;

namespace cstorage_file.ViewModel
{
    public class viewModel : notify_property
    {
        public viewModel()
        {
            _reminder = new StringBuilder();
            _reminder.Append("启动");
        }

        public string reminder
        {
            set
            {
                _reminder.Clear();
                _reminder.Append(value);
                OnPropertyChanged("reminder");
            }
            get
            {
                return _reminder.ToString();
            }
        }
        public async void ce()
        {
            StorageFolder storageFolder = KnownFolders.PicturesLibrary;
            reminder = storageFolder.Name;
            string file = "1.txt";
            Windows.Foundation.IAsyncOperation<StorageFile> wait_storageFile = storageFolder.CreateFileAsync(file , CreationCollisionOption.ReplaceExisting);
            StorageFile storageFile = await wait_storageFile;
            reminder = string.Format("名字{0} {1}" , storageFile.Name , storageFile.Path);

            if (!string.IsNullOrEmpty(reminder) && storageFile != null)
            {
                await FileIO.WriteTextAsync(storageFile , reminder);
                reminder = "启动";
            }
            reminder = await FileIO.ReadTextAsync(storageFile);


            StorageFolder folder =
    Windows.Storage.ApplicationData.Current.LocalFolder;
            storageFile = await folder.CreateFileAsync(file);         
            reminder = folder.Path;
            await FileIO.WriteTextAsync(storageFile , reminder);
            reminder =string.Format("名字：{0} {1}，内容：{2}",storageFile.Name,storageFile.Path, await FileIO.ReadTextAsync(storageFile));
        }
        public async void open()
        {
            Windows.Storage.Pickers.FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".txt");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                reminder = "选择: " + file.Name + "\n" + await FileIO.ReadTextAsync(file);
            }
            else
            {
                reminder = "Operation cancelled.";
            }


        }

        
        private StringBuilder _reminder;
    }
}
