using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tool.Shared.Framework
{
    public abstract class ViewModelBase : IViewModel,INotifyPropertyChanged
    {
        public abstract void OnNavigatedTo(object parameter);
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}