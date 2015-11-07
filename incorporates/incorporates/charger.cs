using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class charger
    {
        public charger(property _property)
        {
            ng_耐久 = new ng_耐久_durable(2000);
            this._property = _property;
        }
        public double num_充电数
        {
            set
            {
                _充电数 = value;
            }
            get
            {
                return _充电数;
            }
        }

        public ng_耐久_durable ng_耐久
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

        public void 充电()
        {

        }
        private ng_耐久_durable _耐久;
        private double _充电数;
        private double _维修费用;
        property _property;
    }
}
