using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace 人
{
    public class cjy_经验
    {
        public cjy_经验()
        {
            jy_经验 = 0;
            maxj_最大经验 = 10;

            d_等级 = 1;
            maxd_最大等级 = 100;

            生成经验表();
        }

        public cjy_经验(void_value 升级)
        {
            jy_经验 = 0;
            maxj_最大经验 = 10;

            d_等级 = 1;
            maxd_最大等级 = 100;
            this.s_升级 = 升级;
            生成经验表();
        }
        public cjy_经验(void_value s_升级 , List<double> 经验表)
        {
            jy_经验 = 0;
            maxj_最大经验 = 10;

            d_等级 = 1;
            maxd_最大等级 = 100;
            this.s_升级 = s_升级;
            this.jyb_经验表 = 经验表;
        }
        ~cjy_经验()
        {

        }

        public List<double> jyb_经验表
        {
            set
            {
                _经验表 = value;
            }
            get
            {
                return _经验表;
            }
        }
        public double maxj_最大经验
        {
            set
            {
                _最大经验 = value;
            }
            get
            {
                return _最大经验;
            }
        }
        public double jy_经验
        {
            set
            {
                //经验超过最大经验
                if (value >= maxj_最大经验)
                {
                    if (d_等级 + 1 < maxd_最大等级)
                    {
                        value -= maxj_最大经验;
                        d_等级++;
                        maxj_最大经验 = jyb_经验表[d_等级];
                        if (s_升级 != null)
                        {
                            s_升级();
                        }
                    }
                }
                _经验 = value;
            }
            get
            {
                return _经验;
            }
        }
        public int maxd_最大等级
        {
            set
            {
                _最大等级 = value;
            }
            get
            {
                return _最大等级;
            }
        }
        public int d_等级
        {
            set
            {
                _等级 = value;
            }
            get
            {
                return _等级;
            }
        }
        public void_value s_升级;
        //public delegate void void_value();

        private int _等级;
        private int _最大等级;

        private double _经验;
        private double _最大经验;
        private List<double> _经验表 = new List<double>();
        private void 生成经验表()
        {
            int i;
            for (i = 0; i < maxd_最大等级; i++)
            {
                if (i < 10)
                {
                    jyb_经验表.Add(i * i * 10);
                }
                else
                {
                    jyb_经验表.Add(i * 100);
                }
            }
        }
    }   
}
