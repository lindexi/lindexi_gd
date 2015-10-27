using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModel
{
    public class viewModel: notify_property
    {
        public viewModel()
        {
            _reminder = new StringBuilder();
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

        private bool integer(double num)
        {
            double decimalnum = 0.000000001;
            int n;
            n = (int)num;
            if (n - decimalnum < num && n + decimalnum > num)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private StringBuilder _reminder;
    }
}
