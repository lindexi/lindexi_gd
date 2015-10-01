using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人.物品
{
    public class cwp_物品_article
    {
        public cwp_物品_article(string name , int 等级 , wp_物品类型 物品类型 ,  long num)
        {
            name_物品名 = name;
            num_数量 = num;
            d_等级 = 等级;
        }

        public string name_物品名
        {
            set
            {
                _name.Clear();
                _name.Append(value);
            }
            get
            {
                return _name.ToString();
            }
        }
        public long num_数量
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
        public string l_类型
        {
            set
            {
                switch (value)
                {
                    case "药":
                    case "药品":
                        _物品类型 = wp_物品类型.药;
                        break;
                    case "装备":
                        _物品类型 = wp_物品类型.装备;
                        break;
                    case "任务":
                        _物品类型 = wp_物品类型.任务;
                        break;
                    default:
                        _物品类型 = wp_物品类型.不知;
                        break;
                }
            }
            get
            {
                return Convert.ToString(_物品类型);
                //return _l.ToString();
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
        public override bool Equals(object obj)
        {
            return ( obj as cwp_物品_article ).name_物品名 == name_物品名 && ( obj as cwp_物品_article ).l_类型 == l_类型;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override string ToString()
        {
            return d_等级.ToString() + "级物品" + name_物品名;
        }

        private StringBuilder _name = new StringBuilder();
        private long _num;
        private int _等级;
        private wp_物品类型 _物品类型;

    }

    public enum wp_物品类型 : int
    {
        不知 = -1,
        药 = 0,
        装备 = 1,
        任务 = 2,
        宝物 = 3,
        原料 = 4
    };

    //public enum wp_物品类型 : int
    //{
    //    不知 = -1,
    //    药 = 0,//药100-200
    //    装备 = 10,//装备10-100
    //    任务 = 1000,//任务1000-10000
    //    宝物=500,//宝物500-1000
    //    原料=10000//原料10000-20000
    //};
}
