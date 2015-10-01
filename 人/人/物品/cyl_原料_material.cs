using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人.物品
{
   public class cyl_原料_material:人.物品.cwp_物品_article
    {
        public cyl_原料_material(string name , int 等级) : base(name , 等级 , 人.物品.wp_物品类型.装备 , 1)
        {

        }
    }
}
