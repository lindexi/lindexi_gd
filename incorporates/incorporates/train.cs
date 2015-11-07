using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    class train
    {
        public double num_电量
        {
            set
            {
                _电量 = value;
            }
            get
            {
                return _电量;
            }
        }
        private double _电量;

    }
}
