using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;

namespace 个人信息数据库
{
    public partial class viewModel: notify_property
    {
        public viewModel()
        {

            SqlConnection sql = new SqlConnection();
        }
        
    }
}
