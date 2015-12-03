using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 个人信息数据库principalComputer.model
{
    /// <summary>
    /// 人物
    /// </summary>
    public class ccontacts : notify_property
    {
        public ccontacts()
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
        /// 通讯人姓名
        /// </summary>
        public string name
        {
            set
            {
                _name = value;
                OnPropertyChanged();
            }
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// 联系方式
        /// </summary>
        public string contact
        {
            set
            {
                _contact = value;
                OnPropertyChanged();
            }
            get
            {
                return _contact;
            }
        }
        /// <summary>
        /// 工作地点
        /// </summary>
        public string address
        {
            set
            {
                _address = value;
                OnPropertyChanged();
            }
            get
            {
                return _address;
            }
        }
        /// <summary>
        /// 城市
        /// </summary>
        public string city
        {
            set
            {
                _city = value;
                OnPropertyChanged();
            }
            get
            {
                return _city;
            }
        }
        /// <summary>
        /// 备注
        /// </summary>
        public string comment
        {
            set
            {
                _comment = value;
                OnPropertyChanged();
            }
            get
            {
                return _comment;
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
                if (name == null)
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
        private string _name;
        private string _contact;
        private string _address;
        private string _city;
        private string _comment;
    }
}
