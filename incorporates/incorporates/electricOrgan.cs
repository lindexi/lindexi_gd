using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class electricOrgan
    {
        public electricOrgan(property _property)
        {
            this._property = _property;
        }
        public double num_发电数
        {
            set
            {
                _发电数_num = value;
            }
            get
            {
                return _发电数_num;
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
                _耐久_durable = value;
            }
            get
            {
                return _耐久_durable;
            }
        }
        public double 发电()
        {
            return instrument.ran.Next((int)num_发电数);
        }
        private double _发电数_num;
        property _property;
        private ng_耐久_durable _耐久_durable;
        private double _维修费用_maintenanceCosts;
    }
}
