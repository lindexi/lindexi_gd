using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class accumulator:ViewModel.notify_property
    {
        public accumulator(property _property)
        {
            this._property = _property;
            ng_耐久 = new ng_耐久_durable(1000);

        }
        public double max_最大电量
        {
            set
            {
                _最大电量_max = value;
            }
            get
            {
                return _最大电量_max;
            }
        }

        public double num_电量
        {
            set
            {
                _电量 = value;
                OnPropertyChanged();
            }
            get
            {
                return _电量;
            }

        }
        public ng_耐久_durable ng_耐久
        {
            set
            {
                _耐久_durable = value;
            }
            get
            {
                return _耐久_durable;
            }
        }
        public double 维修费用
        {
            set
            {
                _维修费用 = value;
            }
            get
            {
                return _维修费用;
            }
        }

        public double 充电(double n)
        {
            num_电量 += n;
            if (num_电量 > max_最大电量)
            {
                n = num_电量 - max_最大电量;
                num_电量 = max_最大电量;
                return n;
            }
            else
            {
                return 0;
            }
        }
        private ng_耐久_durable _耐久_durable;
        private double _最大电量_max;
        private double _电量;
        private double _维修费用;
        property _property;
    }
}
