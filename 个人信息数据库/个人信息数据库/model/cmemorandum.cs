using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 个人信息数据库.model
{
    /// <summary>
    /// 备忘录
    /// </summary>
    public class cmemorandum:notify_property
    {
        public cmemorandum()
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
        /// 时间
        /// </summary>
        public string MTIME
        {
            set
            {
                _mtime = value;
                OnPropertyChanged();
            }
            get
            {
                //1900 / 1 / 1 0:00:00
                if (string.Equals(_mtime , "1900/1/1 0:00:00"))
                {
                    return string.Empty;
                }
                return _mtime;
            }
        }
        /// <summary>
        /// 地点
        /// </summary>
        public string PLACE
        {
            set
            {
                _place = value;
                OnPropertyChanged();
            }
            get
            {
                return _place;
            }
        }
        /// <summary>
        /// 事件
        /// </summary>
        public string incident
        {
            set
            {
                _incident = value;
                OnPropertyChanged();
            }
            get
            {
                return _incident;
            }
        }
        /// <summary>
        /// 人物
        /// </summary>
        public string CONTACTSID
        {
            set
            {
                _CONTACTSID = value;
                OnPropertyChanged();
            }
            get
            {
                return _CONTACTSID;
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
                if (string.IsNullOrEmpty(incident))
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
            cmemorandum memorandum = new cmemorandum();
            return Clone(memorandum);
        }

        public object Clone(cmemorandum memorandum)
        {
            if (memorandum == null)
            {
                memorandum = new cmemorandum();
            }
            memorandum.id = id;
            memorandum.MTIME = MTIME;
            memorandum.PLACE = PLACE;
            memorandum.incident = incident;
            memorandum.CONTACTSID = CONTACTSID;
            return memorandum;
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

            cmemorandum memorandum = obj as cmemorandum;
            if (memorandum.id == id && memorandum.MTIME == MTIME
                && memorandum.PLACE == PLACE
                && memorandum.incident == incident
                && memorandum.CONTACTSID == CONTACTSID)
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
        private string _mtime;
        private string _place;
        private string _incident;
        private string _CONTACTSID;
    }
}
