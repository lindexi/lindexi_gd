using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人
{
    class czb_装备_equipment:ViewModel.notify_property
    {
        public czb_装备_equipment()
        {

        }
        public string name_名字
        {
            get
            {
                return _名字_name;
            }

            set
            {
                _名字_name = value;
                OnPropertyChanged("name_名字");
            }
        }
        public int d_等级_level
        {
            get
            {
                return _等级_level;
            }

            set
            {
                _等级_level = value;
                OnPropertyChanged("d_等级_level");
            }
        }
        public double n_耐久_durable
        {
            get
            {
                return _耐久_durable;
            }

            set
            {
                _耐久_durable = value;
                OnPropertyChanged("n_耐久_durable");
                OnPropertyChanged("percent_耐久度");
            }
        }

        public double max_最大耐久_max_durable
        {
            get
            {
                return _最大耐久_max_durable;
            }

            set
            {
                _最大耐久_max_durable =  value ;
                OnPropertyChanged("max_最大耐久_max_durable");
                OnPropertyChanged("percent_耐久度");
            }
        }

        public double percent_耐久度
        {
            set
            {

            }
            get
            {
                return n_耐久_durable / max_最大耐久_max_durable;
            }
        }        

        private string _名字_name;
        private int _等级_level;
        private double _耐久_durable;
        private double _最大耐久_max_durable;
    }
}
