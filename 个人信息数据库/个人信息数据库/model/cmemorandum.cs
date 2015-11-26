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
    public class cmemorandum
    {
        public cmemorandum()
        {

        }
        /// <summary>
        /// 标识符
        /// </summary>
        public string id
        {
            set;
            get;
        }
        /// <summary>
        /// 时间
        /// </summary>
        public string MTIME
        {
            set;
            get;
        }
        /// <summary>
        /// 地点
        /// </summary>
        public string PLACE
        {
            set;
            get;
        }
        /// <summary>
        /// 事件
        /// </summary>
        public string incident
        {
            set;
            get;
        }
        /// <summary>
        /// 人物
        /// </summary>
        public string CONTACTSID
        {
            set;
            get;
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
