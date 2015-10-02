using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
using Windows.Storage;
using 人;

namespace cdll
{
   public class viewModel:notify_property
    {
        public viewModel()
        {
            _reminder = new StringBuilder();
            reminder = "asdewfz";
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
        public void ce()
        {
            Task thread = new Task(wirte);
            thread.Start();
        }
        public void wirte()
        {
            //c_人 r = new c_人(new Random());

            //FileStream fs = File.Open(@"Y:\1.txt" , FileMode.Create);
            //StringWriter w = new StringWriter(_reminder);
            //StreamWriter s = new StreamWriter(fs);

            ////File.Open(@"Y:\1.txt" , FileMode.Open);

            //w.Write(r.ToString());
            //byte[] buff = Encoding.UTF8.GetBytes(r.ToString());
            //fs.Write(buff , 0 , buff.Length);
            //w.Flush();
            //fs.Flush();
            StorageFile s;
            

            FileStream fs = File.Open(@"1.txt" , FileMode.Open,FileAccess.Read);
            byte[] buff = new byte[1000];
            fs.Read(buff , 0 , buff.Length);
            reminder = buff.ToString();
            OnPropertyChanged("reminder");
        }

        private StringBuilder _reminder;
    }
}
