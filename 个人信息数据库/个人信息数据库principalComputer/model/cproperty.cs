using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 个人信息数据库principalComputer.model
{
    /// <summary>
    /// 个人财物
    /// </summary>
    public class cproperty : notify_property
    {
        public cproperty()
        {
            
        }
        /// <summary>
        /// 标识符
        /// </summary>
        public string id
        {
            set
            {
                _id = value;
                OnPropertyChanged();
            }
            get
            {
                return _id;
            }
        }
        /// <summary>
        /// 项目
        /// </summary>
        public string terminal
        {
            set
            {
                _terminal = value;
                OnPropertyChanged();
            }
            get
            {
                return _terminal;
            }
        }
        /// <summary>
         /// 金额
         /// </summary>  
        public string PMONEY
        {
            set
            {
                _money = value;
                OnPropertyChanged();
            }
            get
            {
                return _money;
            }
        }       
        /// <summary>
        /// 时间
        /// </summary>
        public string MTIME
        {
            set
            {
                _time = value;
                OnPropertyChanged();
            }
            get
            {
                return _time;
            }
        }        
        /// <summary>
        /// 人物
        /// </summary>
        public string CONTACTSID
        {
            set
            {
                _contactsid = value;
                OnPropertyChanged();
            }
            get
            {
                return _contactsid;
            }
        }
        /// <summary>
        /// 输入正确
        /// </summary>
        public bool accord
        {
            set
            {
                value = false;
            }
            get
            {
                int n = 0;
                try
                {
                    n = Convert.ToInt32(PMONEY);
                }
                catch
                {
                    return false;
                }
                
                try
                {
                    DateTime mydate = Convert.ToDateTime(MTIME);
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        private string _id;
        private string _money;
        private string _contactsid;
        private string _time;
        private string _terminal;
    }
}
