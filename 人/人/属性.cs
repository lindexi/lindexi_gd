using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace 人
{
    public class csx_属性
    {
        public csx_属性()
        {
            name_名字 = "";
            min_最小 = 0;
            this.num = 0;
            max_最大数 = 0;
        }
        public csx_属性(string name , double min , double max , double num)
        {
            name_名字 = name;
            min_最小 = min;
            this.num = num;
            max_最大数 = max;
        }
        ~csx_属性()
        {

        }

        public double min_最小
        {
            set
            {
                //value = 0;
                _min = value;
            }
            get
            {
                //return num / 2;
                if (_min > _num)
                {
                    _min = _num;
                }
                return _min;
            }
        }
        public double max_最大数
        {
            set
            {
                //value = 0;
                _max_num = value;
            }
            get
            {
                //return num * 2;
                if (_max_num < _num)
                {
                    _max_num = _num;
                }
                return _max_num;
            }
        }
        public double num
        {
            set
            {
                _num = value;
            }
            get
            {
                return _num;
            }
        }
        public string name_名字
        {

            set
            {
                _name = value;
            }
            get
            {
                return _name;
            }
        }
        public override string ToString()
        {
            return name_名字 + num.ToString() + " ";
        }
        private string _name;
        private double _num;
        private double _max_num;
        private double _min;
    }
}
