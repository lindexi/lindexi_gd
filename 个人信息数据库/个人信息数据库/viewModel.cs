using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;
using 个人信息数据库.model;
namespace 个人信息数据库
{
    public partial class viewModel : notify_property
    {
        public viewModel()
        {

            //SqlConnection sql = new SqlConnection();

        }
        public caddressBook[] addressBook
        {
            set;
            get;
        } =
            {
                //new caddressBook()
                //{
                //    name ="通讯人姓名",
                //    contact ="联系方式",
                //    address ="工作地点",
                //    city ="城市",
                //    comment ="备注"
                //},
                new caddressBook()
                {
                    name ="张三",
                    contact ="1",
                    address ="中国",
                    city =" ",
                    comment =" "
                } ,
                new caddressBook()
                {
                    name ="张三",
                    contact ="1",
                    address ="中国",
                    city =" ",
                    comment =" "
                }
            };
    }
}
