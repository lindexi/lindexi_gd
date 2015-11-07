using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
namespace incorporates
{
    public class viewModel:notify_property
    {
        public viewModel()
        {
            _property = new property();
            _property.PropertyChanged += _property_PropertyChanged;
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
            
        }
        public void 新建集电器()
        {
            p.money -= 新建集电器钱;

        }
        public void 新建充电()
        {
            p.money -= 新建充电钱;

        }
        public void 新建发电器()
        {
            p.money -= 新建发电器钱;

        }

        private double 新建停靠钱;
        private double 新建集电器钱;
        private double 新建充电钱;
        private double 新建发电器钱;


        private property _property;

        
    }
}
