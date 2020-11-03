using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
namespace incorporates
{
    public class viewModel : notify_property, In_下回合_bout
    {
        public viewModel()
        {
            _property = new property();
            _property.PropertyChanged += _property_PropertyChanged;

            集电器 = new accumulator(p);
            充电 = new charger(p);
            停靠 = new passengerTerminal(p);
            发电器 = new electricOrgan(p);

            新建停靠钱 = 1000;
            新建集电器钱 = 1000;
            新建充电钱 = 1000;
            新建发电器钱 = 1000;
        }

        public property p
        {
            set
            {
                _property = value;
            }
            get
            {
                return _property;
            }
        }

        public bool 可以新建停靠
        {
            set
            {
                value = false;
            }
            get
            {
                return p.money >= 新建停靠钱;
            }
        }
        public bool 可以新建集电器
        {
            set
            {
                value = false;
            }
            get
            {
                return p.money >= 新建集电器钱;
            }
        }
        public bool 可以新建充电
        {
            set
            {
                value = false;
            }
            get
            {
                return p.money >= 新建充电钱;
            }
        }
        public bool 可以新建发电器
        {
            set
            {
                value = false;
            }
            get
            {
                return p.money >= 新建发电器钱;
            }
        }

        public string 停靠reminder
        {
            set
            {
                value = string.Empty;
                //OnPropertyChanged();
            }
            get
            {
                StringBuilder str = new StringBuilder();
                str.Append("拥有停车 " + p.停靠.Count.ToString() + "\r\n");
                str.Append("新建需要钱 " + 新建停靠钱.ToString() + "\r\n");
                foreach (var t in p.停靠)
                {
                    str.Append(string.Format("需要电量{0} 停车{1} 停车率{2} 维修费用{3} 耐久{4}/{5}\r\n",t.num_电量,t.berth_停靠,t.per_停靠率,t.maintenanceCosts_维修费用,t.ng_耐久.n_耐久_durable,t.ng_耐久.max_最大耐久_max_durable));
                    if (t.berth_停靠)
                    {
                        str.Append("    电量" + t.t.num_电量.ToString() + "\r\n");
                    }
                    //str.Append(t.reminder);
                    //t.reminder = string.Empty;            
                }
                return str.ToString();
            }
        }
        public string 集电器reminder
        {
            set
            {
                value = string.Empty;
                //OnPropertyChanged();
            }
            get
            {
                StringBuilder str = new StringBuilder();
                str.Append("拥有集电器 " + p.集电器.Count.ToString() + "\r\n");
                str.Append("新建需要钱 " + 新建集电器钱.ToString() + "\r\n");
                foreach (var t in p.集电器)
                {
                    str.Append(string.Format("电量{0}/{1} 维修费用{2} 耐久{3}/{4}\r\n",t.num_电量,t.max_最大电量,t.maintenanceCosts_维修费用,t.ng_耐久.n_耐久_durable,t.ng_耐久.max_最大耐久_max_durable));
                }               
                return str.ToString();
            }
        }
        public string 充电reminder
        {
            set
            {
                value = string.Empty;
                //OnPropertyChanged();
            }
            get
            {
                StringBuilder str = new StringBuilder();
                str.Append("拥有充电 " + p.充电.Count.ToString() + "\r\n");
                str.Append("新建需要钱 " + 新建充电钱.ToString() + "\r\n");
                foreach (var t in p.充电)
                {
                    str.Append(string.Format("电量{0} 维修费用{1} 耐久{2}/{3}\r\n" , t.num_充电数 , t.maintenanceCosts_维修费用 , t.ng_耐久.n_耐久_durable , t.ng_耐久.max_最大耐久_max_durable));
                    //str.Append("电量" + t.num_充电数.ToString() + " 维修费用" + t.maintenanceCosts_维修费用.ToString());
                    //str.Append(" 耐久" + t.ng_耐久.n_耐久_durable.ToString() + "/" + t.ng_耐久.max_最大耐久_max_durable.ToString());
                }
                return str.ToString();
            }
        }
        public string 发电器reminder
        {
            set
            {
                value = string.Empty;
                //OnPropertyChanged();
            }
            get
            {
                StringBuilder str = new StringBuilder();
                str.Append("拥有发电器 " + p.发电器.Count.ToString() + "\r\n");
                str.Append("新建需要钱 " + 新建发电器钱.ToString() + "\r\n");
                foreach (var t in p.发电器)
                {
                    str.Append(string.Format("发电{0} 维修费用{1} 耐久{2}/{3}\r\n" , t.num_发电数 , t.maintenanceCosts_维修费用 , t.ng_耐久.n_耐久_durable , t.ng_耐久.max_最大耐久_max_durable));
                }
                return str.ToString();
            }
        }

        public void 新建停靠()
        {
            p.money -= 新建停靠钱;
            p.停靠.Add(new passengerTerminal(p));
        }
        public void 新建集电器()
        {
            p.money -= 新建集电器钱;
            p.集电器.Add(new accumulator(p));
        }
        public void 新建充电()
        {
            p.money -= 新建充电钱;
            p.充电.Add(new charger(p));
        }
        public void 新建发电器()
        {
            p.money -= 新建发电器钱;
            p.发电器.Add(new electricOrgan(p));
        }

        public void n_下回合()
        {
            ( (In_下回合_bout)_property ).n_下回合();

            OnPropertyChanged("停靠reminder");
            OnPropertyChanged("集电器reminder");
            OnPropertyChanged("充电reminder");
            OnPropertyChanged("发电器reminder");

        }

        private double 新建停靠钱;
        private double 新建集电器钱;
        private double 新建充电钱;
        private double 新建发电器钱;

        private accumulator 集电器;
        private charger 充电;
        private passengerTerminal 停靠;
        private electricOrgan 发电器;

        private property _property;
        private void _property_PropertyChanged(object sender , System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "money")
            {
                OnPropertyChanged("可以新建停靠");
                OnPropertyChanged("可以新建集电器");
                OnPropertyChanged("可以新建充电");
                OnPropertyChanged("可以新建发电器");
            }
        }
    }
}
