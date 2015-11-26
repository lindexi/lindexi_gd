using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 个人信息数据库.model
{
    public class cproperty
    {
        public cproperty()
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
        /// 项目
        /// </summary>
        public string terminal
        {
            set;
            get;
        }
        /// <summary>
        /// 金额
        /// </summary>
        public string PMONEY
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
        /// 人物
        /// </summary>
        public string CONTACTSID
        {
            set;
            get;
        }
    }
}
