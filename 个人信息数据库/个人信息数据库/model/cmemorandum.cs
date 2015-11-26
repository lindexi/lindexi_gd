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
