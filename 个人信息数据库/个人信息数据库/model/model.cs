using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
namespace 个人信息数据库.model
{
    //public class model : notify_property
    //{
    //    public model()
    //    {
    //        ran = new Random();
    //    }
    //    Random ran
    //    {
    //        set;
    //        get;
    //    }
    //    /// <summary>
    //    /// 数据库ip
    //    /// </summary>
    //    public string DataSource
    //    {
    //        set;
    //        get;
    //    } = "QQLINDEXI\\SQLEXPRESS";
    //    /// <summary>
    //    /// 数据库名
    //    /// </summary>
    //    public string InitialCatalog
    //    {
    //        set;
    //        get;
    //    } = "grxx";
    //    /// <summary>
    //    /// 连接数据库
    //    /// </summary>
    //    public string connect
    //    {
    //        set
    //        {
    //            value = string.Empty;
    //        }
    //        get
    //        {
    //            return $"Data Source={DataSource};Initial Catalog={InitialCatalog};Integrated Security=True";
    //        }
    //    }

    //    public void ce()
    //    {
    //        List<caddressBook> addressBook = lajiaddressBook();
    //        //对象转json
    //        var json = JsonConvert.SerializeObject(addressBook);

    //        //writeaddressBook(addressBook);
    //    }

    //    public ObservableCollection<caddressBook> newaddressBook()
    //    {
    //        const string addressBookname = "temp";
    //        string sqlAddressBook = $"{usesql}{line}SELECT * FROM {addressBookname};";
    //        ObservableCollection<caddressBook> addressBook = new ObservableCollection<caddressBook>();
    //        const string id = "id";
    //        const string name = "name";
    //        const string contact = "contact";
    //        const string naddress = "naddress";
    //        const string city = "city";
    //        const string comment = "comment";
    //        using (SqlConnection sql = new SqlConnection(connect))
    //        {
    //            sql.Open();
    //            using (SqlCommand cmd = new SqlCommand(sqlAddressBook , sql))
    //            {
    //                using (SqlDataReader read = cmd.ExecuteReader())
    //                {
    //                    //判断当前的reader是否读取到了数据
    //                    if (!read.HasRows) return addressBook;
    //                    int idindex = read.GetOrdinal(id);
    //                    int nameindex = read.GetOrdinal(name);
    //                    int contactindex = read.GetOrdinal(contact);
    //                    int naddressindex = read.GetOrdinal(naddress);
    //                    int cityindex = read.GetOrdinal(city);
    //                    int commentindex = read.GetOrdinal(comment);
    //                    while (read.Read())
    //                    {
    //                        caddressBook temp = new caddressBook
    //                        {
    //                            id = read.GetInt32(idindex).ToString(),
    //                            name = read.GetString(nameindex).Trim(),
    //                            contact = read.GetString(contactindex).Trim(),
    //                            address = read.GetString(naddressindex).Trim(),
    //                            city = read.GetString(cityindex).Trim(),
    //                            comment = read.GetString(commentindex).Trim()
    //                        };

    //                        addressBook.Add(temp);
    //                    }
    //                }
    //            }
    //        }
    //        return addressBook;
    //    }


    //    /// <summary>
    //    /// 写入通讯录
    //    /// </summary>
    //    /// <param name="addressBook"></param>
    //    public void writeaddressBook(List<caddressBook> addressBook)
    //    {
    //        const string t = "temp";
    //        string strsql;       
    //        foreach (var temp in addressBook)
    //        {
    //            strsql = $"{usesql}{line}insert into {t}(id,name,contact,naddress,city,comment){line}values('{temp.id}','{temp.name}','{temp.contact}','{temp.address}','{temp.city}','{temp.comment}');{line}";
    //            write(strsql);
    //        }
    //    }

    //    /// <summary>
    //    /// 写数据
    //    /// </summary>
    //    /// <param name="strsql"></param>
    //    public void write(string strsql)
    //    {
    //        using (SqlConnection sql = new SqlConnection(connect))
    //        {
    //            sql.Open();
    //            using (SqlCommand cmd = new SqlCommand(strsql , sql))
    //            {
    //                //字符串比char(10)长 将截断字符串或二进制数据
    //                int r = cmd.ExecuteNonQuery();
    //            }
    //        }
    //    }


    //    //public void ce()
    //    //{
    //    //    string inputJsonString = @"
    //    //        [
    //    //            {StudentID:'100',Name:'aaa',Hometown:'china'},
    //    //            {StudentID:'101',Name:'bbb',Hometown:'us'},
    //    //            {StudentID:'102',Name:'ccc',Hometown:'england'}
    //    //        ]";
    //    //    JArray jsonObj = JArray.Parse(inputJsonString);
    //    //    string message = @"<table border='1'>
    //    //            <tr><td width='80'>StudentID</td><td width='100'>Name</td><td width='100'>Hometown</td></tr>";
    //    //    string tpl = "<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>";
    //    //    foreach (JObject jObject in jsonObj)
    //    //    {
    //    //        message += String.Format(tpl , jObject["StudentID"] , jObject["Name"] , jObject["Hometown"]);
    //    //    }
    //    //    message += "</table>";
    //    //    //lbMsg.InnerHtml = message;
    //    //}

    //    private string ranstr(int n)
    //    {
    //        StringBuilder str = new StringBuilder();
    //        int[] 中文 = new int[2] { 19968 , 40895 };
    //        for (int i = 0; i < n; i++)
    //        {
    //            str.Append(Convert.ToChar(ran.Next(中文[0] , 中文[1])));
    //        }
    //        return str.ToString();
    //    }

    //    private string line
    //    {
    //        set;
    //        get;
    //    } = "\n";
    //    private string usesql
    //    {
    //        set
    //        {
    //            value = string.Empty;
    //        }
    //        get
    //        {
    //            return $"use {InitialCatalog}";
    //        }
    //    }

    //    private List<caddressBook> lajiaddressBook()
    //    {
    //        List<caddressBook> addressBook = new List<caddressBook>();
    //        List<string> chinacity = new List<string>();
    //        chinacity.AddRange(sql.城市.Split(new char[2] { '\r' , '\n' }));

    //        for (int i = 0; i < chinacity.Count; i++)
    //        {
    //            if (string.IsNullOrEmpty(chinacity[i]))
    //            {
    //                chinacity.RemoveAt(i);
    //                i--;
    //            }
    //            else
    //            {
    //                chinacity[i] = chinacity[i].Trim();
    //            }
    //        }

    //        int n = 100;
    //        caddressBook temp;

    //        for (int i = 0; i < n; i++)
    //        {
    //            temp = new caddressBook()
    //            {
    //                id = i.ToString() ,
    //                name = ranstr(3) ,
    //                contact = ran.Next().ToString() ,
    //                address = chinacity[ran.Next(chinacity.Count)] ,
    //                city = chinacity[ran.Next(chinacity.Count)] ,
    //                comment = "随机的名，作为测试"
    //            };
    //            addressBook.Add(temp);
    //        }
    //        return addressBook;
    //    }
    //}

    public class model : notify_property
    {
        public model()
        {
            ReceiveAction = (str) =>
            {
                string temp = str.Trim('\0' , ' ');
                if (!string.IsNullOrEmpty(temp))
                {
                    reminder = temp;
                }
            };
            ip = "10.21.71.130";
            access();           
        }
        public int id
        {
            set
            {
                _slaveComputer.id = value;
                OnPropertyChanged();
            }
            get
            {
                return _slaveComputer.id;
            }
        }

        public string ip
        {
            set
            {
                _ip = value;
                OnPropertyChanged();
            }
            get
            {
                return _ip;
            }
        }

        public ObservableCollection<caddressBook> addressbook
        {
            set;
            get;
        } = new ObservableCollection<caddressBook>();

        public ObservableCollection<cproperty> property
        {
            set;
            get;
        } = new ObservableCollection<cproperty>();

        public ObservableCollection<cmemorandum> memorandum
        {
            set;
            get;
        } = new ObservableCollection<cmemorandum>();

        public ObservableCollection<cdiary> diary
        {
            set;
            get;
        } = new ObservableCollection<cdiary>();

        public void ce()
        {
            //_slaveComputer.send(reminder);
            getdata();
        }

        public void getdata()
        {
            ctransmitter transmitter = new ctransmitter(id , ecommand.getdata , string.Empty);
            send(transmitter.ToString());


        }

        public void add<T>(T obj)
        {
            string temp = typeof(T).ToString();
            //int i = temp.LastIndexOf('.');
            //temp = temp.Substring(i + 1);
            ecommand c = ecommand.ce;
            if (string.Equals(temp , typeof(caddressBook).ToString()))
            {
                c = ecommand.addaddressBook;
            }
            else if (string.Equals(temp , typeof(ccontacts).ToString()))
            {
                c = ecommand.addcontacts;
            }
            else if (string.Equals(temp , typeof(cdiary).ToString()))
            {
                c = ecommand.adddiary;
            }
            else if (string.Equals(temp , typeof(cmemorandum).ToString()))
            {
                c = ecommand.addmemorandum;
            }
            else if (string.Equals(temp , typeof(cproperty).ToString()))
            {
                c = ecommand.addproperty;
            }

            string json = JsonConvert.SerializeObject(obj);
            ctransmitter transmitter = new ctransmitter(id , c , json);
            send(transmitter.ToString());
        }

        public void delete(object obj , int id)
        {
            ecommand c = ecommand.ce;
            if (obj as caddressBook != null)
            {
                c = ecommand.daddressBook;
            }
            else if (obj as ccontacts != null)
            {
                c = ecommand.dcontacts;
            }
            else if (obj as cdiary != null)
            {
                c = ecommand.ddiary;
            }
            else if (obj as cmemorandum != null)
            {
                c = ecommand.dmemorandum;
            }
            else if (obj as cproperty != null)
            {
                c = ecommand.dproperty;
            }
            string json = JsonConvert.SerializeObject(id);
            ctransmitter transmitter = new ctransmitter(this.id , c , json);
            send(transmitter.ToString());
        }

        private slaveComputer _slaveComputer;
        private System.Action<string> ReceiveAction;
        private string _ip;

        private void implement(int id , ecommand command , string str)
        {
            try
            {
                switch (command)
                {
                    case ecommand.id:
                        fitid(str);
                        break;
                    case ecommand.addressBook:
                        reminder = "上位机发来通讯录";
                        newaddressBook(str);
                        break;
                    case ecommand.property:
                        reminder = "上位机发来个人财物";
                        newproperty(str);
                        break;
                    case ecommand.diary:
                        reminder = "上位机发来日记";
                        newdiary(str);
                        break;
                    case ecommand.memorandum:
                        reminder = "上位机发来信息";
                        newmemorandum(str);
                        break;
                    default:
                        reminder = str;
                        break;
                }           
                     
            }
            catch (Exception e)
            {
                reminder = "model implement" + e.Message;
            }
        }

        private void newmemorandum(string str)
        {
            var temp = DeserializeObject<cmemorandum>(str);
            System.Windows.Application.Current.Dispatcher.Invoke
              (() =>
              {
                  memorandum.Clear();

                  foreach (var t in temp)
                  {
                      memorandum.Add(t);
                  }
              });
        }
        private void newdiary(string str)
        {
            var temp = DeserializeObject<cdiary>(str);
            System.Windows.Application.Current.Dispatcher.Invoke
               (() =>
               {
                   diary.Clear();

                   foreach (var t in temp)
                   {
                       diary.Add(t);
                   }
               });
        }
        private void newproperty(string str)
        {
            var temp = DeserializeObject<cproperty>(str);
            System.Windows.Application.Current.Dispatcher.Invoke
                (() =>
                {
                    property.Clear();

                    foreach (var t in temp)
                    {
                        property.Add(t);
                    }                    
                });
        }
        private void newaddressBook(string str)
        {
            try
            {
                ObservableCollection<caddressBook> temp = DeserializeObject<caddressBook>(str);

                System.Windows.Application.Current.Dispatcher.Invoke
                (() =>
                {
                    addressbook.Clear();

                    foreach (var t in temp)
                    {
                        addressbook.Add(t);
                    }
                });
            }

            catch (JsonException e)
            {
                reminder = "输入不是ObservableCollection<caddressBook> json" + e.Message;
            }
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
        //private void cleanObservableCollection<T>(ObservableCollection<T> temp)
        //{
        //    if (temp == null)
        //    {
        //        return;
        //    }
        //    for (int i = 0; i < temp.Count;)
        //    {
        //        temp.RemoveAt(i);
        //    }
        //    temp.Clear();
        //}

        /// <summary>
        /// 分配id
        /// </summary>
        /// <param name="id"></param>
        private void fitid(string id)
        {
            try
            {
                int temp = Convert.ToInt32(id);
                this.id = temp;
                reminder = "id" + id;

                getdata();//初始返回data
            }
            catch (Exception e)
            {
                reminder = "输入id不是数字" + e.Message;
            }
        }
        public void send(string str)
        {
            _slaveComputer.send(str);
        }
        public void send(ecommand command , string str)
        {
            ctransmitter transmitter = new ctransmitter(id , command , str);
            send(transmitter.ToString());
        }
        public void access()
        {            
            try
            {
                if (_slaveComputer != null)
                {
                    _slaveComputer.exit();
                }
                _slaveComputer = new slaveComputer(ReceiveAction , implement);

                _slaveComputer.access(ip);
                reminder = "运行";
            }
            catch (System.Net.Sockets.SocketException e)
            {
                reminder = "连接失败，ip错误或服务器没开启 " + e.Message;
            }
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
