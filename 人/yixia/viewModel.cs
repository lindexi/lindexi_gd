using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
namespace yixia
{
    public class viewModel:notify_property
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

        private StringBuilder _reminder;
    }
}
