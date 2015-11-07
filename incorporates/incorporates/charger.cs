using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class charger
    {
        public charger()
        {

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
        public void 充电()
        {

        }
        private double _充电数;
        private double _维修费用;
    }
}
