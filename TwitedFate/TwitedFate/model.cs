using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;

namespace TwitedFate
{
    public class model
    {
        public model()
        {
            ce();
        }

        public void ce()
        {
            // writeasset桌面();
        }

        //设置壁纸
        //复制壁纸到localFolder
        private async void writeasset桌面()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/桌面.jpg"));

            StorageFile tmp = await file.CopyAsync(ApplicationData.Current.LocalFolder , "桌面.jpg" , NameCollisionOption.ReplaceExisting);
             
            bool success = false;
            //检查是否支持
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetWallpaperImageAsync(tmp);

            }
        }
    }
}
