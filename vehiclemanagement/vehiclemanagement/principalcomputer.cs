using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sqllindexi
{
    public static class principalcomputer
    {
        /// <summary>
        /// 数据库ip
        /// </summary>
        public static string DataSource
        {
            set;
            get;
        } = "QQLINDEXI\\SQLEXPRESS";
        /// <summary>
        /// 数据库名
        /// </summary>
        public static string InitialCatalog
        {
            set;
            get;
        } = "grxx";


        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="strsql"></param>
        public static string write(string strsql)
        {
            StringBuilder str = new StringBuilder();
            using (SqlConnection sql = new SqlConnection(connect))
            {
                sql.Open();
                using (SqlCommand cmd = new SqlCommand(strsql , sql))
                {
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        while (read.Read())
                        {
                            try
                            {
                                for (int i = 0; i < read.FieldCount; i++)
                                {
                                    Type t = read.GetFieldType(i);
                                    str.Append(read.GetValue(i).ToString().PadRight(10));
                                }
                                str.Append("\n");
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
            return str.ToString();
        }

        /// <summary>
        /// 输出
        /// </summary>
        /// <param name="str"></param>
        public static void printf(string str)
        {
            if (str as string != null)
            {
                Console.Write(str + "\n");
            }
        }



        public static string DBNullstring<T>(object obj)
        {
            try
            {
                return obj == System.DBNull.Value ? " " : ( (T)obj ).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 连接数据库
        /// </summary>
        private static string connect
        {
            set
            {
                value = string.Empty;
            }
            get
            {
                return $"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security=True";
            }
        }
    }
}
