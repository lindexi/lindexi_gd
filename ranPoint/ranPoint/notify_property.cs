using System.ComponentModel;

namespace ViewModel
{
    /// <summary>
    /// 提供继承通知UI改变值
    /// </summary>
    public class notify_property : INotifyPropertyChanged
    {
        public notify_property()
        {

        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void UpdateProper<T>(ref T properValue , T newValue , [System.Runtime.CompilerServices.CallerMemberName] string properName = "")
        {
            if (object.Equals(properValue , newValue))
                return;

            properValue = newValue;
            OnPropertyChanged(properName);
        }
        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name="")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this , new PropertyChangedEventArgs(name));
            }
        }
    }
}
