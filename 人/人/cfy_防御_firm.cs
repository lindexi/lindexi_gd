using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人
{
    class cfy_防御_firm : ViewModel.notify_property
    {
        public cfy_防御_firm()
        {
           
        }
        public cfy_防御_firm(Random ran , clan_生命魔力体力_liftMagicStrength 量 , StringBuilder reminder_日志)
        {
            lan_量 = 量;
            _ran = ran;
            this.reminder_日志 = reminder_日志;
        }
        public cfy_防御_firm(Random ran, ct_条 魔力 , ct_条 体力, StringBuilder reminder_日志)
        {
            _魔力_magic = 魔力;
            _体力_stamina = 体力;
            _ran = ran;
            this.reminder_日志 = reminder_日志;
        }

        public StringBuilder reminder_日志
        {
            get
            {
                return _reminder_日志;
            }

            set
            {
                _reminder_日志 = value;
            }
        }

        /// <summary>
        /// 
        /// 装备抵挡，消耗体力
        /// 直接减去自己防御
        ///             
        /// </summary>
        /// <param name="gj_受到攻击"></param>
        /// <returns>受到的伤害</returns>
        public double fy_遭受攻击(double gj_受到攻击)
        {
            
            return 0;
        }
        private StringBuilder _reminder_日志;
        private Random _ran;
        private ct_条 _魔力_magic;
        private ct_条 _体力_stamina;
        private clan_生命魔力体力_liftMagicStrength lan_量;

        
    }
}
