using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class ng_耐久_durable:ViewModel.notify_property
    {
        public ng_耐久_durable(double 耐久)
        {
            max_最大耐久_max_durable = n_耐久_durable = 耐久;
        }
        public double n_耐久_durable
        {
            set
            {
                _耐久_durable = value;
                OnPropertyChanged("n_耐久_durable");
                OnPropertyChanged("percent_耐久度");
            }
            get
            {
                return _耐久_durable;
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
                _最大耐久_max_durable = value;
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


        private double _耐久_durable;
        private double _最大耐久_max_durable;
    }
}
