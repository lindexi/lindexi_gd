using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人
{
    public class clan_生命魔力体力_liftMagicStrength
    {
        public clan_生命魔力体力_liftMagicStrength()
        {
            lan_拥有的所有栏.Add(new ct_条("生命" , 100 , 100 , 0));
            lan_拥有的所有栏.Add(new ct_条("魔力" , 100 , 100 , 0));
            lan_拥有的所有栏.Add(new ct_条("体力" , 100 , 100 , 0));
        }
        public List<ct_条> lan_拥有的所有栏
        {
            set
            {
                _拥有的所有栏 = value;
            }
            get
            {
                return _拥有的所有栏;
            }
        }
        public ct_条 s_生命_lift
        {
            get
            {
                return getLan_获得所要的栏("生命");
            }
        }
        public ct_条 m_魔力_magic
        {
            get
            {
                return getLan_获得所要的栏("魔力");
            }
        }
        public ct_条 t_体力_magic
        {
            get
            {
                return getLan_获得所要的栏("体力");                
            }
        }
        //精力

        /// <summary>
        /// 添加一个 精力 
        /// </summary>
        /// <param name="add">要添加栏</param>
        /// <returns>x相同名返回false </returns>
        public bool add_添加栏_add_bar(ct_条 add)
        {
            foreach (ct_条 temp in lan_拥有的所有栏)
            {
                if (temp.name_名字 == add.name_名字)
                {
                    return false;
                }
            }
            lan_拥有的所有栏.Add(add);
            return true;
        }
        private ct_条 getLan_获得所要的栏(string name)
        {
            ct_条 所要的栏 = null;
            foreach (ct_条 temp in lan_拥有的所有栏)
            {
                if (temp.name_名字 == name)
                {
                    所要的栏 = temp;
                }
            }
            return 所要的栏;
        }
        private List<ct_条> _拥有的所有栏 = new List<ct_条>();
    }
}
