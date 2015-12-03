using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 个人信息数据库.model
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
                //1900 / 1 / 1 0:00:00
                if (string.Equals(_time , "1900/1/1 0:00:00"))
                {
                    return string.Empty;
                }
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
                decimal n = 0;
                try
                {
                    n = Convert.ToDecimal(PMONEY);
                }
                catch
                {
                    return false;
                }
              
               
                try
                {
                    if (!string.IsNullOrEmpty(MTIME))
                    {
                        DateTime mydate = Convert.ToDateTime(MTIME);
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        public object Clone()
        {
            cproperty property = new cproperty();
            return Clone(property);
        }

        public object Clone(cproperty property)
        {
            if (property == null)
            {
                property = new cproperty();
            }
            property.id = id;
            property.MTIME = MTIME;
            property.PMONEY = PMONEY;
            property.terminal = terminal;
            property.CONTACTSID = CONTACTSID;
            return property;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            // TODO: write your implementation of Equals() here

            cproperty property = obj as cproperty;
            if (property.id == id && property.MTIME == MTIME
                && property.terminal == terminal
                && property.PMONEY == PMONEY
                && property.CONTACTSID == CONTACTSID)
            {
                return true;
            }

            return false;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here

            return base.GetHashCode();
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
