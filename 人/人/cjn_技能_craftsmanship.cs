using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人
{
    class cjn_技能_craftsmanship:ViewModel.notify_property,In_下回合_bout
    {
        public cjn_技能_craftsmanship()
        {
            d_当前冷却 = 0;
        }

        public string name_名字
        {
            get
            {
                return _名字_name;
            }

            set
            {
                _名字_name = value;
            }
        }

        public double x_消耗魔力_consumption
        {
            get
            {
                return _消耗魔力_consumption;
            }

            set
            {
                _消耗魔力_consumption =  value ;
            }
        }

        public double jlq_技能冷却_CD
        {
            get
            {
                return _技能冷却_CD;
            }

            set
            {
                _技能冷却_CD =  value ;
            }
        }

        public double glq_公共冷却_public_CD
        {
            get
            {
                return _公共冷却_public_CD;
            }

            set
            {
                _公共冷却_public_CD =  value ;
            }
        }

        public double d_当前冷却
        {
            get
            {
                return _当前冷却;
            }

            set
            {
                _当前冷却 =  value ;
                OnPropertyChanged("d_当前冷却");
            }
        }
        public bool c_可以释放
        {
            get
            {
                return d_当前冷却 >= glq_公共冷却_public_CD + jlq_技能冷却_CD;
            }
        }

        public void n_下回合()
        {
            if (c_可以释放)
            {
            }
            else
            {
                d_当前冷却++;
            }
        }
        private string _名字_name;
        //消耗魔力
        private double _当前冷却;
        private double _消耗魔力_consumption;
        private double _技能冷却_CD;
        private double _公共冷却_public_CD;

        void In_下回合_bout.n_下回合()
        {
            n_下回合();
        }
    }
}
