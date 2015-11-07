using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class property:ViewModel.notify_property,In_下回合_bout
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
        public string 总电量
        {
            set
            {
                OnPropertyChanged("总电量");
            }
            get
            {
                double f, d,m;
                f = 0;
                d = 0;
                m = 0;
                foreach (var t in _发电器)
                {
                    f += t.num_发电数;
                }
                foreach (accumulator t in _集电器)
                {
                    d += t.num_电量;
                    m += t.max_最大电量;
                }
                return "电量 "+d.ToString()+"/"+m.ToString() + " 发电量 " + f.ToString();
            }
        }
        

        private List<accumulator> _集电器 = new List<accumulator>();
        private List<charger> _充电 = new List<charger>();
        private List<passengerTerminal> _停靠 = new List<passengerTerminal>();
        private List<electricOrgan> _发电器 = new List<electricOrgan>();
        
        private double _money;

        void In_下回合_bout.n_下回合()
        {
            throw new NotImplementedException();
        }
    }
}
