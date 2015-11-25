using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;
using 个人信息数据库.model;
using System.Windows.Threading;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using System.Collections.ObjectModel;

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
            _model = new model.model();
            reminder = "运行";

            ReceiveAction = str =>
            {
                string temp= str.Trim('\0' , ' ');
                if (!string.IsNullOrEmpty(temp))
                {
                    reminder = temp;
                }
            };

            slave_computer();
        }

        /// <summary>
        /// 读取sql
        /// </summary>
        //public void readsql()
        //{
            //string fileaddress = @"data/sql/插入数据.sql";
            //string strsql;
            //Encoding encoding = Encoding.Default;

            //using (FileStream file = new FileStream(fileaddress , FileMode.Open))
            //{
            //    int length = (int)file.Length;
            //    byte[] buff = new byte[length];
            //    file.Read(buff , 0 , length);
            //    strsql = encoding.GetString(buff);
            //}

            //string strsql = Properties.Resources.插入数据;
            //_model.refreshData(strsql);
            //reminder = "插入" + Properties.Resources.插入数据;

            //_model.ce();
        //}

        public void ce()
        {
            //_model.ce();

            //addressBook = _model.newaddressBook();

            string json = JsonConvert.SerializeObject(addressBook);
            //reminder = json;
            //addressBook = new System.Collections.ObjectModel.ObservableCollection<caddressBook>();
            //addressBook = JsonConvert.DeserializeObject<ObservableCollection<caddressBook>>(json);
            if (_sx上位机下位机 == 上位机下位机.x下位机)
            {
                if (_slaveComputer != null)
                {
                    _slaveComputer.send(new ctransmitter(_slaveComputer.id , ecommand.ce , json).ToString());
                }
            }
            else
            {
                if (_principal_Computer != null)
                {
                    _principal_Computer.send(new ctransmitter(-1 , ecommand.ce , json).ToString());
                }
            }


            //string connect = $"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security=True";
            ////List<caddressBook> addressBook = new List<caddressBook>();
            //using (SqlConnection sql = new SqlConnection(connect))
            //{
            //    sql.Open();
            //    if (sql.State == System.Data.ConnectionState.Closed)
            //    {
            //        sql.Open();
            //    }
            //    if (sql.State == System.Data.ConnectionState.Open)
            //    {
            //        reminder = string.Empty;
            //        reminder = "打开数据库";
            //    }

            //    //构建sql语句
            //    string strsql;
            //    string line = "\n";
            //    string usesql = $"use {InitialCatalog}";//使用数据库
            //    strsql = $"{usesql}{line}insert into temp(id,name,contact,naddress,city,comment){line}values('1','张三','1','中国',' ',' ');{line}";

            //    //执行sql语句需要一个"命令对象"
            //    //创建一个命令对象
            //    using (SqlCommand cmd = new SqlCommand(strsql , sql))
            //    {
            //        int r = cmd.ExecuteNonQuery();
            //        reminder = string.Empty;
            //        reminder = $"成功插入了{r.ToString()}";

            //        //执行sql语句
            //        //cmd.ExecuteNonQuery() //当执行insert,delete,update语句时，一般使用该方法


            //        //当执行返回单个值的sql语句时使用该方法。
            //        //cmd.ExecuteScalar()


            //        //当执行Sql语句返回多行多列时，一般使用该方法。
            //        //cmd.ExecuteReader()
            //    }

            //    //查询
            //    strsql = $"SELECT * FROM temp";
            //    //列
            //    string id = "id";
            //    string name = "name";
            //    string contact = "contact";
            //    string naddress = "naddress";
            //    string city = "city";
            //    string comment = "comment";
            //    using (SqlCommand cmd = new SqlCommand(strsql , sql))
            //    {
            //        using (SqlDataReader read = cmd.ExecuteReader())
            //        {
            //            //判断当前的reader是否读取到了数据
            //            if (read.HasRows)
            //            {
            //                int idindex = read.GetOrdinal(id);
            //                int nameindex = read.GetOrdinal(name);
            //                int contactindex = read.GetOrdinal(contact);
            //                int naddressindex = read.GetOrdinal(naddress);
            //                int cityindex = read.GetOrdinal(city);
            //                int commentindex = read.GetOrdinal(comment); 
            //                while (read.Read())
            //                {
            //                    caddressBook temp = new caddressBook();
            //                    temp.name = read.GetString(nameindex);
            //                    temp.contact = read.GetString(contactindex);
            //                    temp.address = read.GetString(nameindex);
            //                    temp.city = read.GetString(cityindex);
            //                    temp.comment = read.GetString(commentindex);

            //                    addressBook.Add(temp);
            //                }
            //                reminder = "查询";
            //            }                        
            //        }
            //    }

            //}

            //SqlConnectionStringBuilder connStrbuilder = new SqlConnectionStringBuilder();
            //connStrbuilder.DataSource = DataSource;
            //connStrbuilder.InitialCatalog = InitialCatalog;
            //connStrbuilder.IntegratedSecurity = true;
            //connStrbuilder.Pooling = true;
            //reminder = "打开数据库";

        }

        public void principal_computer()
        {
            //_sx上位机下位机 = 上位机下位机.s上位机;
            //_principal_Computer = new principal_Computer(ReceiveAction);
        }

        public void slave_computer()
        {
            _sx上位机下位机 = 上位机下位机.x下位机;
            _slaveComputer = new slaveComputer(ReceiveAction);
            _slaveComputer.access("10.21.71.130");
        }

       //private void AddItem(object item)
       // {
       //     addressBook.Add(item as caddressBook);
       // }

        public System.Collections.ObjectModel.ObservableCollection<caddressBook>
        /*public List<caddressBook>*/ addressBook
        {
            set;
            get;
        } =
           //new List<caddressBook>()
           new System.Collections.ObjectModel.ObservableCollection<caddressBook>()
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
        private model.model _model;

        private principal_Computer _principal_Computer;
        private slaveComputer _slaveComputer;
        private System.Action<string> ReceiveAction;
        private 上位机下位机 _sx上位机下位机;
    }
    enum 上位机下位机
    {
        s上位机,
        x下位机
    }
}
