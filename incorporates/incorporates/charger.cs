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

            num_充电数 = instrument.ran.Next(10);
            maintenanceCosts_维修费用 = 100;
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
        public double maintenanceCosts_维修费用
        {
            set
            {
                _维修费用_maintenanceCosts = value;
            }
            get
            {
                return _维修费用_maintenanceCosts;
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
            if (_train == null)
            {
                foreach (var t in _property.停靠)
                {
                    if (t.t.充电 == null)
                    {
                        t.t.充电 = this;
                        _train = t.t;
                        break;
                    }
                }
            }
            else
            {
                if (_train.num_电量 <= 0)
                {
                    _train = null;
                }
                else
                {
                    _train.num_电量 -= num_充电数;
                }
            }
        }
        private ng_耐久_durable _耐久;
        private double _充电数;
        private double _维修费用_maintenanceCosts;
        private train _train;
        property _property;
    }
}
