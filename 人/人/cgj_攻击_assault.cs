using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人
{
    class cgj_攻击_assault:ViewModel.notify_property,In_下回合_bout
    {
        public cgj_攻击_assault()
        {
            //_魔力= new ct_条("魔力" , 100 , 100 , 0);
            //_体力= new ct_条("体力" , 100 , 100 , 0);
            _ran = new Random();
            reminder_日志 = new StringBuilder();
            _一回合攻击行动力 = 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ran"></param>
        /// <param name="量">必须</param>
        /// <param name="reminder_日志"></param>
        public cgj_攻击_assault(Random ran , clan_生命魔力体力_liftMagicStrength 量 , StringBuilder reminder_日志)
        {
            lan_量 = 量;
            this.reminder_日志 = reminder_日志;
            this.一回合攻击行动力 = 一回合攻击行动力;
        }

        public cgj_攻击_assault(Random ran ,int 一回合攻击行动力, ct_条 魔力 , ct_条 体力 , StringBuilder reminder_日志)
        {
            _魔力 = 魔力;
            _体力 = 体力;
            _ran = ran;
            this.reminder_日志 = reminder_日志;
            this.一回合攻击行动力 = 一回合攻击行动力;
        }

        public int 一回合攻击行动力
        {
            get
            {
                return _一回合攻击行动力;
            }

            set
            {
                _一回合攻击行动力 = value;
            }
        }
        public bool xd_行动力
        {
            set
            {
                if (value)
                {
                    _行动力 = _一回合攻击行动力;
                }
                else
                {
                    _行动力 = 0;
                }
            }
            get
            {
                return _行动力 > 0;
            }
        }

        public StringBuilder reminder_日志
        {
            set
            {
                _reminder_日志 = value;
                OnPropertyChanged("reminder_日志");
            }
            get
            {
                return _reminder_日志;
            }
        }

        /// <summary>
        /// 发出攻击，武器攻击，自身攻击
        /// 行动false不攻击
        /// </summary>
        /// <returns>攻击大小</returns>
        public double gj_攻击()
        {
            if (xd_行动力)
            {
                //体力减发出攻击

                _行动力--;
            }
            else
            {
                return 0;
            }
            return 0;
        }
        public void n_下回合()
        {

        }

        void In_下回合_bout.n_下回合()
        {
            n_下回合(); 
        }
        private int _行动力;
        private int _一回合攻击行动力;
        private StringBuilder _reminder_日志;
        private Random _ran;
        private ct_条 _魔力;
        private ct_条 _体力;
        private clan_生命魔力体力_liftMagicStrength lan_量;
    }
}
