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
                foreach (var t in 发电器)
                {
                    f += t.num_发电数;
                }
                foreach (accumulator t in 集电器)
                {
                    d += t.num_电量;
                    m += t.max_最大电量;
                }
                return "电量 "+d.ToString()+"/"+m.ToString() + " 发电量 " + f.ToString();
            }
        }

        public List<accumulator> 集电器
        {
            set
            {
                _集电器 = value;
            }
            get
            {
                return _集电器;
            }
        }

        public List<charger> 充电
        {
            set
            {
                _充电 = value;
            }
            get
            {
                return _充电;
            }
        }

        public List<passengerTerminal> 停靠
        {
            set
            {
                _停靠 = value;
            }
            get
            {
                return _停靠;
            }

        }

        public List<electricOrgan> 发电器
        {
            set
            {
                _发电器 = value;
            }
            get
            {
                return _发电器;
            }

        }

        private List<accumulator> _集电器 = new List<accumulator>();
        private List<charger> _充电 = new List<charger>();
        private List<passengerTerminal> _停靠 = new List<passengerTerminal>();
        private List<electricOrgan> _发电器 = new List<electricOrgan>();
        
        private double _money;
        

        void In_下回合_bout.n_下回合()
        {
            总电量 = string.Empty;

        }
    }
}
