using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人.装备
{
    public class czb_装备_gear: 人.物品.cwp_物品_article
    {
        public czb_装备_gear(string name , int 等级, zbl_装备类型 装备类型) : base(name , 等级 , 人.物品.wp_物品类型.装备 , 1)
        {
            _经验 = new cjy_经验(升级);
            _装备类型 = 装备类型;
        }

        public ct_条 nj_耐久_durable
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
        /// <summary>
        /// 装备状态
        /// 良好 破旧 损坏
        /// </summary>
        public string zt_状态
        {
            set
            {

            }
            get
            {
                if (nj_耐久_durable.percent > 60)
                {
                    return "良好";
                }
                else if (nj_耐久_durable.percent > 10)
                {
                    return "破旧";
                }
                else
                {
                    return "损坏";
                }
            }
        }

        public cjy_经验 jy_经验
        {
            set
            {
                _经验 = value;
            }
            get
            {
                return _经验;
            }
        }

        private cjy_经验 _经验;

        private ct_条 _耐久 = new ct_条("耐久" , 100 , 100 , 0);
        private zbl_装备类型 _装备类型;
        private void 升级()
        {

        }
    }
    public enum zbl_装备类型 : int
    {
        头 = 1,
        肩 = 2,
        衣服 = 3,
        裤子 = 4,
        鞋子 = 5,
        武器=6
    };
}
