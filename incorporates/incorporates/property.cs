using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class property:ViewModel.notify_property
    {
        public property()
        {

        }
        public double money
        {
            set
            {
                _money = value;
                OnPropertyChanged();
            }
            get
            {
                return _money;
            }
        }

        private double _money;

        
    }
}
