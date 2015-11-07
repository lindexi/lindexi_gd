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

        private List<accumulator> _集电器 = new List<accumulator>();
        private List<charger> _充电 = new List<charger>();
        private List<passengerTerminal> _停靠 = new List<passengerTerminal>();
        private List<electricOrgan> _发电器 = new List<electricOrgan>();

        private double _money;

        
    }
}
