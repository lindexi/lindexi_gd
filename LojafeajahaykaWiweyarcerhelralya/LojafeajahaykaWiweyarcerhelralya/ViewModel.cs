using System.Collections.ObjectModel;

namespace LojafeajahaykaWiweyarcerhelralya
{
    public class ViewModel
    {
        public ViewModel()
        {
            for (int i = 0; i < 100; i++)
            {
                Collection.Add(i.ToString());
            }
        }

        public ObservableCollection<string> Collection { get; }  = new ObservableCollection<string>();
    }
}