using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
namespace incorporates
{
    public class viewModel:notify_property, In_下回合_bout
    {
        public viewModel()
        {
            _property = new property();
            _property.PropertyChanged += _property_PropertyChanged;

            集电器 = new accumulator(p);
            充电 = new charger(p);
            停靠 = new passengerTerminal(p);
            发电器 = new electricOrgan(p);
        }

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
            }
            get
            {
                return p.money >= 新建发电器钱;
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

        
    }
}
