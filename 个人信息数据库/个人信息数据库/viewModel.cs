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
            //连接字符串

            //使用windows身份验证方式
            //string constr = "Data Source=steve-pc;Initial Catalog=itcast2013;Integrated Security=true";
            //"server=.;database=itcast2013;uid=sa;pwd=sa"

            reminder = "运行";


        }

        public void ce()
        {
            string connect = $"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security=True";
            using (SqlConnection sql = new SqlConnection(connect))
            {
                sql.Open();
                if (sql.State == System.Data.ConnectionState.Closed)
                {
                    sql.Open();
                }
                if (sql.State == System.Data.ConnectionState.Open)
                {
                    reminder = string.Empty;
                    reminder = "打开数据库";
                }
            }

            //SqlConnectionStringBuilder connStrbuilder = new SqlConnectionStringBuilder();
            //connStrbuilder.DataSource = DataSource;
            //connStrbuilder.InitialCatalog = InitialCatalog;
            //connStrbuilder.IntegratedSecurity = true;
            //connStrbuilder.Pooling = true;
            //reminder = "打开数据库";

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

        private string DataSource
        {
            set;
            get;
        } = "QQLINDEXI\\SQLEXPRESS";
        private string InitialCatalog
        {
            set;
            get;
        } = "grxx";
    }
}
