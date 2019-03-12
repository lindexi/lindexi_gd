using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 个人信息数据库.model
{
    public class cdiary : notify_property,ICloneable
    {
        public cdiary()
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
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public object Clone()
        {
            cdiary diary= new cdiary();            
            return Clone(diary);
        }

        public object Clone(cdiary diary)
        {
            if (diary == null)
            {
                diary = new cdiary();
            }
            diary.id = id;
            diary.MTIME = MTIME;
            diary.PLACE = PLACE;
            diary.incident = incident;
            diary.CONTACTSID = CONTACTSID;
            return diary;
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

            cdiary diary = obj as cdiary;
            if (diary.id == id && diary.MTIME == MTIME
                && diary.PLACE == PLACE
                && diary.incident == incident
                && diary.CONTACTSID == CONTACTSID)
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
        
        
       
        private string _id;
        private string _mtime;
        private string _place;
        private string _incident;
        private string _CONTACTSID;
    }
}
