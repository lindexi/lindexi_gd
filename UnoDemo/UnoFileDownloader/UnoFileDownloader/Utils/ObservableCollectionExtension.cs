using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnoFileDownloader.Utils
{
    public static class ObservableCollectionExtension
    {
        public static void AddRange<T>(this ObservableCollection<T> observableCollection, IEnumerable<T> list)
        {
            foreach (var temp in list)
            {
                observableCollection.Add(temp);
            }
        }
    }
}
