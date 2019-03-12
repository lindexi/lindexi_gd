using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gameT
{
    public class c : INotifyPropertyChanged
    {
        int i;
        public event PropertyChangedEventHandler PropertyChanged;
        public c()
        {
            i = 1;
            write = i.ToString();
        }
        public static c g_获得类()
        {
            if (_c == null)
            {
                _c = new c();
            }
            return _c;
        }

        public string write
        {
            set
            {
                _write = value;
                //time();
                OnPropertyChanged("write");
            }
            get
            {
                return _write;
            }
        }

        public void time()
        {            
            write = (++i).ToString();
        }
        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this , new PropertyChangedEventArgs(name));
            }
        }
        private string _write;
        private static c _c = new c();


    }
}
