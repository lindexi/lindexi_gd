using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人.物品
{
    public class chc_合成_synthesis
    {
        public chc_合成_synthesis()
        {
            _原料 = new List<cyl_原料_material>();
        }
        public chc_合成_synthesis(List<cyl_原料_material> 原料)
        {
            _原料 = 原料;
        }

        private List<cyl_原料_material> _原料;
    }
}
