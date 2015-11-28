using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
namespace 个人信息数据库principalComputer.model
{
    public class model : notify_property
    {
        public model()
        {
            ran = new Random();
            _principal_computer = new principal_Computer(str =>
              {
                  string temp = str.Trim('\0' , ' ');
                  if (!string.IsNullOrEmpty(temp))
                  {
                      reminder = temp;
                  }
              } , implement);

            ce();
        }
        Random ran
        {
            set;
            get;
        }
        /// <summary>
        /// 数据库ip
        /// </summary>
        public string DataSource
        {
            set;
            get;
        } = "QQLINDEXI\\SQLEXPRESS";
        /// <summary>
        /// 数据库名
        /// </summary>
        public string InitialCatalog
        {
            set;
            get;
        } = "grxx";
        /// <summary>
        /// 连接数据库
        /// </summary>
        public string connect
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

        public void ce()
        {
            //List<caddressBook> addressBook = lajiaddressBook();
            //对象转json
            //var json = JsonConvert.SerializeObject(addressBook);

            //writeaddressBook(addressBook);
        }

        //public void add<T>(T obj)
        //{
        //    string temp = typeof(T).ToString();
        //    int i = temp.LastIndexOf('.');
        //    reminder = temp.Substring(i+1);

        //}



        public ObservableCollection<caddressBook> newaddressBook()
        {
            const string addressBookname = "vaddressbook";
            string sqlAddressBook = $"{usesql}{line}SELECT * FROM {addressBookname};";
            ObservableCollection<caddressBook> addressBook = new ObservableCollection<caddressBook>();
            const string id = "id";
            const string name = "name";
            const string contact = "contact";
            const string caddress = "caddress";
            const string city = "city";
            const string comment = "comment";
            using (SqlConnection sql = new SqlConnection(connect))
            {
                sql.Open();
                using (SqlCommand cmd = new SqlCommand(sqlAddressBook , sql))
                {
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        //判断当前的reader是否读取到了数据
                        if (!read.HasRows)
                            return addressBook;
                        int idindex = read.GetOrdinal(id);
                        int nameindex = read.GetOrdinal(name);
                        int contactindex = read.GetOrdinal(contact);
                        int caddressindex = read.GetOrdinal(caddress);
                        int cityindex = read.GetOrdinal(city);
                        int commentindex = read.GetOrdinal(comment);
                        while (read.Read())
                        {
                            caddressBook temp = new caddressBook
                            {
                                id = read.GetInt32(idindex).ToString() ,
                                name = read.GetString(nameindex).Trim() ,
                                contact = read.GetString(contactindex).Trim() ,
                                address = read.GetString(caddressindex).Trim() ,
                                city = read.GetString(cityindex).Trim() ,
                                comment = read.GetString(commentindex).Trim()
                            };
                            addressBook.Add(temp);
                        }
                    }
                }
            }
            return addressBook;
        }


        /// <summary>
        /// 写入通讯录
        /// </summary>
        /// <param name="addressBook"></param>
        public void writeaddressBook(List<caddressBook> addressBook)
        {
            string strsql;

            //addressbook CONTACTS
            const string addressbook = "addressbook";
            const string contacts = "CONTACTS";
            string id;
            foreach (var temp in addressBook)
            {
                strsql = $"{usesql}{line}insert into {contacts}(name,contact,caddress,city,comment){line}values('{temp.name}','{temp.contact}','{temp.address}','{temp.city}','{temp.comment}') SELECT @@IDENTITY AS Id;";
                id = write(strsql);
                strsql = $"insert into {addressbook}(CONTACTSID) values( '{id}');";
                write(strsql);
            }
        }

        /// <summary>
        /// 全部更新为最新数据
        /// </summary>
        public void getdata()
        {
            //返回addressBook
            ObservableCollection<caddressBook> addressBook = newaddressBook();
            string json = JsonConvert.SerializeObject(addressBook);
            ctransmitter transmitter = new ctransmitter(-1 , ecommand.addressBook , json);
            _principal_computer.send(transmitter.ToString());


        }

        /// <summary>
        /// 添加通讯录
        /// </summary>
        public void addaddressBook(caddressBook addressbook)
        {
            //添加加上一个在末尾
            string strsql;
            const string addressBook = "addressbook";
            const string contacts = "CONTACTS";
            string id;

            if (addressbook == null)
            {
                reminder = "添加通讯录，添加的通讯录空";
                return;
            }

            strsql = $"{usesql}{line}insert into {contacts}(name,contact,caddress,city,comment){line}values('{addressbook.name}','{addressbook.contact}','{addressbook.address}','{addressbook.city}','{addressbook.comment}') SELECT @@IDENTITY AS Id;";
            id = write(strsql);
            strsql = $"insert into {addressBook}(CONTACTSID) values( '{id}');";
            write(strsql);
        }
        /// <summary>
        /// 删除通讯录
        /// </summary>
        /// <param name="id">要删除id</param>
        public void deleteaddressBook(caddressBook addressbook)
        {
            string strsql;
            string id;
            const string addressBook = "addressbook";
            const string contacts = "CONTACTS";
            strsql = $"{usesql}{line}SELECT CONTACTSID{line}FROM {addressBook}{line}WHERE ID='{addressbook.id}';";
            id = write(strsql);

            //DELETE FROM ADDRESSBOOK
            //WHERE addressBook.ID = '213';
            strsql = $"{usesql}{line}DELETE FROM {addressBook}{line}WHERE {addressBook}.ID = '{addressbook.id}';";
            write(strsql);

            //DELETE FROM CONTACTS
            //WHERE CONTACTS.ID = '218';
            strsql = $"{usesql}{line}DELETE FROM {contacts}{line}WHERE {contacts}.ID = '{id}';";
            write(strsql);

            reminder = "删除" + addressbook.id + " " + addressbook.name;
        }

        //修改 DataGrid 




        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="strsql"></param>
        public string write(string strsql)
        {
            using (SqlConnection sql = new SqlConnection(connect))
            {
                sql.Open();
                using (SqlCommand cmd = new SqlCommand(strsql , sql))
                {
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        try
                        {
                            if (!read.HasRows)
                                return null;
                            const string id = "id";
                            int idindex = read.GetOrdinal(id);
                            while (read.Read())
                            {
                                return read.GetDecimal(0).ToString();
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            return null;
        }


        //public void ce()
        //{
        //    string inputJsonString = @"
        //        [
        //            {StudentID:'100',Name:'aaa',Hometown:'china'},
        //            {StudentID:'101',Name:'bbb',Hometown:'us'},
        //            {StudentID:'102',Name:'ccc',Hometown:'england'}
        //        ]";
        //    JArray jsonObj = JArray.Parse(inputJsonString);
        //    string message = @"<table border='1'>
        //            <tr><td width='80'>StudentID</td><td width='100'>Name</td><td width='100'>Hometown</td></tr>";
        //    string tpl = "<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>";
        //    foreach (JObject jObject in jsonObj)
        //    {
        //        message += String.Format(tpl , jObject["StudentID"] , jObject["Name"] , jObject["Hometown"]);
        //    }
        //    message += "</table>";
        //    //lbMsg.InnerHtml = message;
        //}

        private string ranstr(int n)
        {
            StringBuilder str = new StringBuilder();
            int[] 中文 = new int[2] { 19968 , 40895 };
            for (int i = 0; i < n; i++)
            {
                str.Append(Convert.ToChar(ran.Next(中文[0] , 中文[1])));
            }
            return str.ToString();
        }

        private string line
        {
            set;
            get;
        } = "\n";
        private string usesql
        {
            set
            {
                value = string.Empty;
            }
            get
            {
                return $"use {InitialCatalog};";
            }
        }
        private string go
        {
            set
            {
                value = string.Empty;
            }
            get
            {
                return $"{line}GO{line}";
            }
        }
        private principal_Computer _principal_computer;

        private List<caddressBook> lajiaddressBook()
        {
            List<caddressBook> addressBook = new List<caddressBook>();
            List<string> chinacity = new List<string>();
            chinacity.AddRange(sql.城市.Split(new char[2] { '\r' , '\n' }));

            for (int i = 0; i < chinacity.Count; i++)
            {
                if (string.IsNullOrEmpty(chinacity[i]))
                {
                    chinacity.RemoveAt(i);
                    i--;
                }
                else
                {
                    chinacity[i] = chinacity[i].Trim();
                }
            }

            int n = 100;
            caddressBook temp;

            for (int i = 0; i < n; i++)
            {
                temp = new caddressBook()
                {
                    id = i.ToString() ,
                    name = ranstr(3) ,
                    contact = ran.Next().ToString() ,
                    address = chinacity[ran.Next(chinacity.Count)] ,
                    city = chinacity[ran.Next(chinacity.Count)] ,
                    comment = "随机的名，作为测试"
                };
                addressBook.Add(temp);
            }
            return addressBook;
        }

        private void lajidiary()
        {

        }

        private void implement(int id , ecommand command , string str)
        {
            caddressBook addressbook;
            switch (command)
            {
                case ecommand.ce://2015年11月26日08:56:10
                    break;
                case ecommand.getdata:
                    getdata();
                    reminder = id.ToString() + "获取数据";
                    return;                    
                case ecommand.addaddressBook:
                    addressbook = Deserialize<caddressBook>(str);
                    addaddressBook(addressbook);
                    reminder = id.ToString() + "添加通讯录";
                    break;
                case ecommand.newaddressBook:
                    newaddressBook(str);                   
                    break;
                case ecommand.daddressBook:
                    addressbook = Deserialize<caddressBook>(str);
                    deleteaddressBook(addressbook); 
                    break;
                default:
                    reminder = str;
                    break;
            }
            getdata();
        }

        private void newaddressBook(string str)
        {
            caddressBook temp = Deserialize<caddressBook>(str);
            string strsql;

            strsql = $"{usesql}{line}UPDATE CONTACTS{line}SET NAME='{temp.name}',CONTACT='{temp.contact}',CADDRESS='{temp.address}',CITY='{temp.city}',COMMENT='{temp.comment}'{line}WHERE ID IN (SELECT CONTACTSID FROM addressBook WHERE ID='{temp.id}');";
            write(strsql);

            reminder = "修改通讯录";

        }
        private T Deserialize<T>(string str)
        {
            try
            {
                T temp = JsonConvert.DeserializeObject<T>(str);
                return temp;
            }
            catch (JsonException e)
            {
                reminder = "输入不是ObservableCollection<caddressBook> json" + e.Message;
            }
            return default(T);
        }

        private ObservableCollection<T> DeserializeObject<T>(string str)
        {
            try
            {
                ObservableCollection<T> temp = JsonConvert.DeserializeObject<ObservableCollection<T>>(str);
                return temp;
            }
            catch (JsonException e)
            {
                reminder = "输入不是ObservableCollection<caddressBook> json" + e.Message;
            }
            return null;
        }

    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public class ctransmitter
    {
        public ctransmitter(int id , ecommand command , string str/*,int ran*/)
        {
            this.id = id.ToString();
            this.command = command.ToString();
            this.str = str;
            //this.ran = ran.ToString();
        }

        public string id
        {
            set;
            get;
        }
        public string command
        {
            set;
            get;
        }
        public string str
        {
            set;
            get;
        }
        //public string ran
        //{
        //    set;
        //    get;
        //}
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public enum ecommand
    {
        login,//登录
        id,//分配id
        //get,//发送成功
        getdata,//获取

        addressBook,//返回通讯录
        contacts,
        property,
        diary,
        memorandum,

        addaddressBook,//add
        addcontacts,
        adddiary,
        addmemorandum,
        addproperty,

        daddressBook,
        dcontacts,
        ddiary,
        dproperty,
        dmemorandum,

        saddressBook,
        scontacts,
        sdiary,
        sproperty,
        smemorandum,

        newaddressBook,//通讯录
        newcontacts,//人物
        newproperty,
        newdiary,
        newmemorandum,



        ce,


    }


}
