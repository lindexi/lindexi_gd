using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    class passengerTerminal
    {

        public ng_耐久_durable 耐久
        {
            set
            {
                _耐久 = value;
            }
            get
            {
                return _耐久;
            }
        }

        public bool 停靠
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

        public double 停靠率
        {
            set
            {
                _停靠率 = value;
            }
            get
            {
                return _停靠率;
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

        private ng_耐久_durable _耐久;
        private bool _停靠;
        private double _停靠率;
        private double _维修费用;
    }
}
