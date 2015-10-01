using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace 人
{
    public class c_人: ViewModel.notify_property
    {
        public c_人()
        {
            初始化_new_class();
        }

        public c_人(Random ran)
        {
            this._ran = ran;
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

        public int d_等级_level
        {
            get
            {
                return _等级_level;
            }

            set
            {
                _等级_level = value;
            }
        }

        public ct_条 s_生命_lift
        {
            set
            {
                _生命 = value;
            }
            get
            {
                return _生命;
            }
        }

        public double g_攻击_strength
        {
            get
            {
                return _攻击_strength;
            }

            set
            {
                _攻击_strength =  value ;
            }
        }

        public double f_防御_firm
        {
            get
            {
                return _防御_firm;
            }

            set
            {
                _防御_firm =  value ;
            }
        }

        public StringBuilder reminder_日志
        {
            get
            {
                return _reminder_日志;
            }

            set
            {
                _reminder_日志 =  value ;
            }
        }

        public double w_悟性_savv
        {
            get
            {
                return _悟性;
            }

            set
            {
                _悟性 =  value ;
            }
        }

        public double q_潜力_potential
        {
            get
            {
                return _潜力;
            }

            set
            {
                _潜力 =  value ;
            }
        }

        public double ml_魅力_attractive
        {
            set
            {
                _魅力 = value;
            }

            get
            {
                return _魅力;
            }            
        }

        public override string ToString()
        {
            return d_等级_level + "级 " + name_名字 + " " + s_生命_lift.ToString();
        }

        //伤口
        //流血
        private string _名字_name;
        private int _等级_level;
        private ct_条 _生命;
        private double _攻击_strength;
        private double _防御_firm;
        private Random _ran;
        private StringBuilder _reminder_日志;
        private double _悟性;
        private double _潜力;
        private double _魅力;//没有属性2015年9月29日10:27:18
        private void 初始化_new_class()
        {
            this._ran = new Random();
        }
    }

    public class cr_人 : IComparable
    {
        public cr_人(Random _ran)
        {
            this._ran = _ran;
            初始化();
        }
        public cr_人(int d_等级 , int jy_经验 , double g_攻击 , double y_远程攻击 ,
            int yrange_远程攻击范围 , double f_防御 , double s_生命 , double 恢复 , double m_魔力 , double t_体力 , double q_潜力 , int 一回合攻击行动力 , string name)
        {
            this.d_等级 = d_等级;
            this.jy_经验.jy_经验 = jy_经验;
            this.g_攻击.min_最小 = g_攻击 / 2;
            this.g_攻击.num = g_攻击;
            this.g_攻击.max_最大数 = g_攻击 * 2;
            this.y_远程攻击.num = y_远程攻击;
            this.y_远程攻击.max_最大数 = y_远程攻击 * 2;
            this.y_远程攻击.min_最小 = y_远程攻击 / 2;
            this.yrange_远程攻击范围 = yrange_远程攻击范围;
            this.f_防御.num = f_防御;
            this.f_防御.min_最小 = f_防御 / 2;
            this.f_防御.max_最大数 = f_防御 * 2;
            this.s_生命.num = s_生命;
            this.s_生命.max_最大数 = s_生命;
            this.s_生命.恢复 = 恢复;
            this.m_魔力.max_最大数 = m_魔力;
            this.m_魔力.num = m_魔力;
            this.t_体力.max_最大数 = t_体力;
            this.t_体力.num = t_体力;
            this.q_潜力 = q_潜力;
            _一回合攻击行动力 = 一回合攻击行动力;
            name_名字 = name;
        }
        public cr_人(string name , int d_等级 , double g_攻击 , double f_防御 , double s_生命 , double s_恢复 , double m_魔力 , double m_恢复 , double t_体力 , double t_恢复 , double q_潜力 , int 一回合攻击行动力 , Random _ran)
        {
            this._ran = _ran;
            _经验 = new cjy_经验(new void_value(升级));
            this.name_名字 = name;
            this.d_等级 = d_等级;
            this.g_攻击.num = g_攻击;
            this.f_防御.num = f_防御;
            this.s_生命.max_最大数 = s_生命;
            this.s_生命.num = s_生命;
            this.s_生命.恢复 = s_恢复;
            this.m_魔力.max_最大数 = m_魔力;
            this.m_魔力.num = m_魔力;
            this.m_魔力.恢复 = m_恢复;
            this.t_体力.max_最大数 = t_体力;
            this.t_体力.num = t_体力;
            this.t_体力.恢复 = t_恢复;
            this.q_潜力 = q_潜力;
            this._一回合攻击行动力 = 一回合攻击行动力;
        }
        ~cr_人()
        {

        }
        public csx_属性 g_攻击
        {
            set
            {
                _攻击 = value;
            }
            get
            {
                return _攻击;
            }
        }
        public csx_属性 f_防御
        {
            set
            {
                _防御 = value;
            }
            get
            {
                return _防御;
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
        public ct_条 s_生命
        {
            set
            {
                _生命 = value;
            }
            get
            {
                return _生命;
            }
        }
        public ct_条 m_魔力
        {
            set
            {
                _魔力 = value;
            }
            get
            {
                return _魔力;
            }
        }
        //怒气;
        public ct_条 t_体力
        {
            set
            {
                _体力 = value;
            }
            get
            {
                return _体力;
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
        public int d_等级
        {
            set
            {
                jy_经验.d_等级 = value;
            }
            get
            {
                return jy_经验.d_等级;
            }
        }
        /// <summary>
        /// 敌人数
        /// </summary>
        public int dr_敌人
        {
            set
            {
                _敌人 = value;
            }
            get
            {
                return _敌人;
            }
        }
        /// <summary>
        /// 行动力true可以攻击，攻击了行动力=false
        /// _行动力>0 体力 true
        /// </summary>
        public bool xd_行动力
        {
            set
            {
                if (value == true)
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
                if (_行动力 <= 0)
                {
                    reminder += "没有行动力 ";
                }
                if (t_体力.num <= 0)
                {
                    reminder += "没有体力 ";
                }
                return ( _行动力 > 0 && t_体力.num > 0 );
            }
        }
        /// <summary>
        /// 每受攻击加1，受攻击>=6行动力true;
        /// </summary>
        public int sg_受攻击
        {
            set
            {
                if (value >= 6)
                {
                    value = 0;
                    xd_行动力 = true;
                }
                _受攻击 = value;
            }
            get
            {
                return _受攻击;
            }
        }
        /// <summary>
        /// 一回合攻击行动力，一回合两次攻击 _一回合攻击行动力=2
        /// </summary>
        public int 一回合攻击行动力
        {
            set
            {
                _一回合攻击行动力 = value;
            }
            get
            {
                return _一回合攻击行动力;
            }
        }
        /// <summary>
        /// 加经验增加对方等级
        /// </summary>
        public int wx_悟性
        {
            set
            {
                _悟性 = value;
            }
            get
            {
                return _悟性;
            }
        }
        public bool 自己加潜力
        {
            set
            {
                _自己加潜力 = value;
            }
            get
            {
                return _自己加潜力;
            }
        }
        public bool sj_升级
        {
            set
            {
                _升级 = value;
            }
            get
            {
                return _升级;
            }
        }
        /// <summary>
        /// 和一人打加一
        /// </summary>
        public int 经验
        {
            get
            {
                return _jy_经验;
            }
            set
            {
                _jy_经验 = value;
            }
        }
        //public double gj_攻击
        //{
        //    private set
        //    {
        //        value = 0;
        //    }
        //    get
        //    {
        //        return ran.Next((int)g_攻击.min_最小 , (int)g_攻击.max_最大数) + (d_等级-1) * q_潜力;
        //    }
        //}
        public csx_属性 y_远程攻击
        {
            set
            {
                _远程攻击 = value;
            }
            get
            {
                return _远程攻击;
            }
        }
        public int yrange_远程攻击范围
        {
            set
            {
                _远程攻击范围 = value;
            }
            get
            {
                return _远程攻击范围;
            }
        }
        public double fy_防御
        {
            private set
            {
                value = 0;
            }
            get
            {
                return ran.Next((int)f_防御.min_最小 , (int)f_防御.max_最大数) + ( d_等级 - 1 ) * q_潜力;
            }
        }
        public double q_潜力
        {
            set
            {
                _潜力 = value;
            }
            get
            {
                return _潜力;
            }
        }
        /// <summary>
        /// 按比例伤害
        /// </summary>
        public double p_比例伤害
        {
            set
            {
                _比例伤害 = value;
            }
            get
            {
                if (_比例伤害 > 1)
                {
                    while (_比例伤害 >= 1)
                    {
                        _比例伤害 /= 10;
                    }
                }
                return _比例伤害;
            }
        }
        /// <summary>
        /// 强行伤害
        /// </summary>
        public double b_必伤害
        {
            set
            {
                _必伤害 = value;
            }
            get
            {
                return _必伤害;
            }
        }

        public Random ran
        {
            set
            {
                _ran = value;
            }
            get
            {
                return _ran;
            }
        }
        /// <summary>
        /// 战斗记录
        /// </summary>
        public string reminder
        {
            set
            {
                _reminder.Clear();
                _reminder.Append(value);

            }
            get
            {
                return _reminder.ToString();
            }
        }
        /// <summary>
        /// 当前有祝福
        /// </summary>
        public List<czf_祝福> zf_当前有祝福
        {
            set
            {
                _当前有祝福 = value;
            }
            get
            {
                return _当前有祝福;
            }
        }

        public int sds_杀敌数
        {
            set
            {
                _杀敌数 = value;
            }
            get
            {
                return _杀敌数;
            }
        }

        public void n_下回合()
        {
            //sg_受攻击 = 0;
            xd_行动力 = true;
            //if (s_生命.num < s_生命.max_最大数)
            //{
            //    s_生命.num += s_生命.恢复;
            //}
            //else
            //{
            //    s_生命.num = s_生命.max_最大数;
            //}
            //if (m_魔力.num < m_魔力.max_最大数)
            //{
            //    m_魔力.num += m_魔力.恢复;
            //}
            //else
            //{
            //    m_魔力.num = m_魔力.max_最大数;
            //}
            //if (t_体力.num < t_体力.max_最大数)
            //{
            //    t_体力.num += t_体力.恢复;
            //}
            //else
            //{
            //    t_体力.num = t_体力.max_最大数;
            //}

            foreach (ct_条 t in _恢复)
            {
                if (t.num < t.max_最大数)
                {
                    t.num += t.恢复;
                }
                else
                {
                    t.num = t.max_最大数;
                }
            }
            //祝福 2015年8月28日19:05:32
            for (int i = 0; i < zf_当前有祝福.Count; i++)
            {
                foreach (ct_条 t in _恢复)
                {
                    if (t.name_名字 == zf_当前有祝福[i].zy_祝福作用)
                    {
                        t.num += zf_当前有祝福[i].dx_祝福大小;
                        zf_当前有祝福[i].cx_持续时间--;
                        if (zf_当前有祝福[i].cx_持续时间 <= 0)
                        {
                            zf_当前有祝福.RemoveAt(i);
                            i--;
                        }
                        break;
                    }
                }
            }
            reminder = "";
        }
        //真正的可以修改只有攻击和遭受攻击

        /// <summary>
        /// 攻击对方一次
        /// </summary>
        /// <param name="对方"></param>
        /// <returns>对方伤害</returns>
        public double gj_攻击(cr_人 对方)
        {
            if (xd_行动力 == true)
            {
                double 攻击 = ran.Next((int)g_攻击.min_最小 , (int)g_攻击.max_最大数);//+ (d_等级 - 1) * q_潜力;
                //reminder += "攻击 " + 攻击.ToString()+" ";
                _行动力--;
                t_体力.num -= 攻击;

                攻击 = 对方.z_遭受攻击(攻击);
                reminder += this.name_名字 + "对" + 对方.name_名字 + "造成伤害 " + 攻击.ToString() + " "
                    + "加经验 " + jy_加经验(对方.d_等级 , 攻击).ToString() + " ";
                _log.Append(this.name_名字 + "对" + 对方.name_名字 + "造成伤害 " + 攻击.ToString() + " " + "加经验 " + jy_加经验(对方.d_等级 , 攻击).ToString() + " ");
                return 攻击;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 计算对方攻击伤害，增加 sg_受攻击
        /// </summary>
        /// <param name="g_攻击">对方发出攻击</param>
        /// <returns>遭受攻击大小</returns>
        public double z_遭受攻击(double g_攻击)
        {
            //switch (g_攻击.name_名字)
            //{
            //    case "攻击":
            //        {
            //            double hurt;
            //            hurt = g_攻击.num - fy_防御;
            //            if (hurt < 0)
            //            {
            //                hurt = 0;
            //                return false;
            //            }
            //            s_生命.num -= hurt;
            //            sg_受攻击++;
            //        }
            //        break;
            //    default:
            //        break;
            //}
            double hurt;
            hurt = g_攻击 - fy_防御;

            if (hurt < 0)
            {
                hurt = 0;
                return hurt;
            }
            else
            {
                reminder += "遭受攻击 " + g_攻击.ToString() + "\n" +
                    "造成伤害 " + hurt.ToString() + "\n";
                _log.Append("遭受攻击 " + g_攻击.ToString() + "\n" +
                    "造成伤害 " + hurt.ToString() + "\n");
            }
            s_生命.num -= hurt;
            //sg_受攻击++;
            return hurt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="攻击到"></param>
        /// <returns>攻击到经验</returns>
        public double jy_加经验(cr_人 攻击到 , double 输出)
        {
            double temp;
            temp = 输出 * 攻击到.d_等级 / d_等级;
            jy_经验.jy_经验 += temp;
            return temp;
        }

        /// <summary>
        /// 如果没杀死对方
        /// </summary>
        /// <param name="攻击到等级">攻击到对方等级</param>
        /// <returns>攻击到经验</returns>
        public double jy_加经验(int 攻击到等级 , double 输出)
        {
            double temp;
            攻击到等级 += wx_悟性;
            temp = 0;
            if (攻击到等级 >= d_等级)
            {
                temp = 输出 * 攻击到等级 / d_等级;
            }
            else
            {
                temp = 输出 / ( d_等级 - 攻击到等级 );
            }
            jy_经验.jy_经验 += temp;
            return temp;
        }
        /// <summary>
        /// 每一个级 经验*(ran(10)+1)
        /// </summary>
        /// <param name="攻击到等级">攻击到对方等级</param>
        /// <returns>攻击到经验</returns>
        public double jy_加经验(int 攻击到等级)
        {
            int i, j;
            i = 0;
            j = 1;
            if (攻击到等级 > d_等级)
            {
                i = 攻击到等级 - d_等级;

                for (; i > 0; i--)
                {
                    j *= ( ran.Next(10) + 1 );
                }
                jy_经验.jy_经验 += ( 攻击到等级 + wx_悟性 - d_等级 ) * j;
            }
            else
            {
                jy_经验.jy_经验 += 攻击到等级;
            }
            return 攻击到等级;
        }
        public override string ToString()
        {
            StringBuilder tostring = new StringBuilder();
            tostring.Append("名字 " + name_名字 + "\n");
            tostring.Append("等级" + d_等级 + "\n");
            tostring.Append(s_生命.ToString() + "\n");
            tostring.Append(g_攻击.ToString() + "\n");
            tostring.Append(f_防御.ToString() + "\n");
            return tostring.ToString();
        }
        private void 升级()
        {
            if (!自己加潜力)
            {
                s_生命.max_最大数 += q_潜力 * 10;
                s_生命.恢复 += q_潜力;
                m_魔力.max_最大数 += q_潜力 * 10;
                m_魔力.恢复 += q_潜力;
                t_体力.max_最大数 += q_潜力 * 100;
                t_体力.恢复 += q_潜力;
                g_攻击.num += q_潜力;
                f_防御.num += q_潜力 / 2;
            }
            else
            {
                sj_升级 = true;
                q_潜力 += 10;
            }
        }
        private bool _升级;
        int IComparable.CompareTo(object obj)
        {
            int result;
            try
            {
                cr_人 人 = obj as cr_人;
                if (this.d_等级 > 人.d_等级)
                {
                    result = -1;
                }
                else if (this.d_等级 < 人.d_等级)
                {
                    result = 1;
                }
                else
                {
                    if (this.jy_经验.jy_经验 > 人.jy_经验.jy_经验)
                    {
                        result = -1;
                    }
                    else if (this.jy_经验.jy_经验 < 人.jy_经验.jy_经验)
                    {
                        result = 1;
                    }
                    else
                    {
                        result = 0;
                    }
                }
                // Info info = obj as Info;                 
                //if (this.Id > info.Id)
                //{
                //     result = 0;
                // }
                // else
                //     result = 1;
                // return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }
        private bool 初始化()
        {
            _经验 = new cjy_经验(new void_value(升级));
            _reminder = new StringBuilder();
            jy_经验.d_等级 = 1;
            //d_等级 = 1;
            jy_经验.jy_经验 = 0;
            //经验 = 0;
            _潜力 = 0;
            _一回合攻击行动力 = 1;
            _自己加潜力 = false;
            _升级 = false;

            _恢复.Add(s_生命);
            _恢复.Add(m_魔力);
            _恢复.Add(t_体力);
            return true;
        }
        private bool _自己加潜力;
        private int _敌人;
        private string _name;
        private double _潜力;
        private int _远程攻击范围;
        private int _jy_经验;
        private double _必伤害;
        private double _比例伤害;
        private int _受攻击;
        private int _行动力;
        private int _一回合攻击行动力;
        private int _悟性;
        private int _杀敌数;
        private StringBuilder _reminder;
        private StringBuilder _log = new StringBuilder();//战斗记录
        private Random _ran;// = new Random();
        private ct_条 _生命 = new ct_条("生命" , 100 , 100 , 0);
        private ct_条 _魔力 = new ct_条("魔力" , 100 , 100 , 0);
        private ct_条 _体力 = new ct_条("体力" , 100 , 100 , 0);
        private cjy_经验 _经验; //= new cjy_经验();
        private csx_属性 _攻击 = new csx_属性("攻击" , 1 , 10 , 6);
        private csx_属性 _防御 = new csx_属性("防御" , 1 , 10 , 6);
        private csx_属性 _远程攻击 = new csx_属性("远程攻击" , 0 , 0 , 0);
        private List<ct_条> _恢复 = new List<ct_条>();
        private List<czf_祝福> _当前有祝福 = new List<czf_祝福>();
    }
    /// <summary>
    /// 喝红，加祝福次数是一
    /// 狂，祝福次数持续
    /// 中毒，祝福大小-祝福大小
    /// </summary>
    public class czf_祝福
    {
        public czf_祝福()
        {

        }
        public czf_祝福(double 祝福大小 , int 持续时间 , string 祝福作用)
        {
            this.dx_祝福大小 = 祝福大小;
            this.cx_持续时间 = 持续时间;
            this.zy_祝福作用 = 祝福作用;
        }
        private int _持续时间;
        private double _祝福大小;
        private string _祝福作用;//生命？攻击 魔力 增加 减少
        public int cx_持续时间
        {
            set
            {
                _持续时间 = value;
            }
            get
            {
                return _持续时间;
            }
        }

        public double dx_祝福大小
        {
            set
            {
                _祝福大小 = value;
            }
            get
            {
                return _祝福大小;
            }
        }
        /// <summary>
        /// 生命、魔力、体力
        /// </summary>
        public string zy_祝福作用
        {
            set
            {
                _祝福作用 = value;
            }
            get
            {
                return _祝福作用;
            }
        }
    }
}
