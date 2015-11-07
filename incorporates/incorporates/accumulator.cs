using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class accumulator:ViewModel.notify_property
    {
        public accumulator()
        {

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

        public bool 充电()
        {

        }
        private double _最大电量_max;
        private double _电量;
        private double _维修费用;
    }
}
