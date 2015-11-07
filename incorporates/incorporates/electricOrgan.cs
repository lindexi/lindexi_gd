using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    class electricOrgan
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
        public void 发电()
        {

        }
        private double _发电数_num;
        property _property;

    }
}
