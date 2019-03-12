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
using System.Windows;
using 个人信息数据库.ViewModel;
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
            _model.PropertyChanged += _model_PropertyChanged;
            //reminder = "运行";

            //ReceiveAction = str =>
            //{
            //    string temp= str.Trim('\0' , ' ');
            //    if (!string.IsNullOrEmpty(temp))
            //    {
            //        reminder = temp;
            //    }
            //};

            //slave_computer();            
        }

        public new string reminder
        {
            set
            {
                _model.reminder = value;
            }
            get
            {
                return _model.reminder;
            }
        }

        public Visibility vaddressbook
        {
            set
            {
                if (addressbook != null)
                {
                    addressbook.visibility = value;
                    OnPropertyChanged();
                }
            }
            get
            {
                if (addressbook == null)
                {
                    return Visibility.Hidden;
                }
                else
                {
                    return addressbook.visibility;
                }
            }
        }
        public Visibility vreminder
        {
            set
            {
                _vreminde = value;
                OnPropertyChanged("vreminder");
                OnPropertyChanged("buttonvisibility");
            }
            get
            {
                return _vreminde;
            }
        }
        public Visibility vdiary
        {
            set
            {
                if (diary != null)
                {
                    diary.visibility = value;
                    OnPropertyChanged();
                }
            }
            get
            {
                if (diary != null)
                {
                    return diary.visibility;
                }
                return Visibility.Hidden;
            }
        }
        public Visibility vmemorandum
        {
            set
            {
                if (memorandum != null)
                {
                    memorandum.visibility = value;
                    OnPropertyChanged();
                }
            }
            get
            {
                if (memorandum != null)
                {
                    return memorandum.visibility;
                }
                return Visibility.Hidden;
            }
        }
        public Visibility vproperty
        {
            set
            {
                if (property != null)
                {
                    property.visibility = value;
                    OnPropertyChanged();
                }
            }
            get
            {
                if (property != null)
                {
                    return property.visibility;
                }
                return Visibility.Hidden;
            }
        }
        public Visibility buttonvisibility
        {
            set
            {
                OnPropertyChanged();
            }
            get
            {
                if (vreminder == Visibility.Visible)
                {
                    return Visibility.Hidden;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

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

        //private string DataSource
        //{
        //    set;
        //    get;
        //} = "QQLINDEXI\\SQLEXPRESS";
        //private string InitialCatalog
        //{
        //    set;
        //    get;
        //} = "grxx";
        public model.model _model
        {
            set;
            get;
        }

        public void ce()
        {
            _model.ce();

            //addressBook = _model.newaddressBook();

            //string json = JsonConvert.SerializeObject(addressBook);
            //_model.ce();

            //reminder = json;
            //addressBook = new System.Collections.ObjectModel.ObservableCollection<caddressBook>();
            //addressBook = JsonConvert.DeserializeObject<ObservableCollection<caddressBook>>(json);
            //if (_sx上位机下位机 == 上位机下位机.x下位机)
            //{
            //    if (_slaveComputer != null)
            //    {
            //        _slaveComputer.send(new ctransmitter(_slaveComputer.id , ecommand.ce , json).ToString());
            //    }
            //}
            //else
            //{
            //    if (_principal_Computer != null)
            //    {
            //        _principal_Computer.send(new ctransmitter(-1 , ecommand.ce , json).ToString());
            //    }
            //}


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
            //_sx上位机下位机 = 上位机下位机.x下位机;
            //_slaveComputer = new slaveComputer(ReceiveAction);
            //_slaveComputer.access("10.21.71.130");
            _model.access();
           
        }

        public void form(visibilityform visibility)
        {
            vaddressbook = Visibility.Hidden;
            vreminder = Visibility.Hidden;
            vdiary = Visibility.Hidden;
            vmemorandum = Visibility.Hidden;
            vproperty = Visibility.Hidden;
            _visibilityform = visibility;
            switch (visibility)
            {
                case visibilityform.addressbook:
                    vaddressbook = Visibility.Visible;
                    addressbook.navigated();
                    break;
                case visibilityform.diary:
                    vdiary = Visibility.Visible;
                    diary.navigated();
                    break;
                case visibilityform.memorandum:
                    vmemorandum = Visibility.Visible;
                    memorandum.navigated();
                    break;
                case visibilityform.property:
                    vproperty = Visibility.Visible;
                    property.navigated();
                    break;
                case visibilityform.reminder:
                    vreminder = Visibility.Visible;
                    break;
                default:
                    vreminder = Visibility.Visible;
                    break;
            }
        }

        public void add()
        {
            switch (_visibilityform)
            {
                case visibilityform.addressbook:
                    addressbook.add();
                    break;
                case visibilityform.diary:
                    diary.add();
                    break;
                case visibilityform.memorandum:
                    memorandum.add();
                    break;
                case visibilityform.property:
                    property.add();
                    break;
                case visibilityform.reminder:
                    reminder = "界面错误";
                    break;
                default:
                    reminder = "界面错误";
                    break;
            }
            //_model.getdata();
        }

        public void delete()
        {
            switch (_visibilityform)
            {
                case visibilityform.addressbook:
                    addressbook.delete();
                    break;
                case visibilityform.diary:
                    diary.delete();
                    break;
                case visibilityform.memorandum:
                    memorandum.delete();
                    break;
                case visibilityform.property:
                    property.delete();
                    break;
                case visibilityform.reminder:
                    reminder = "界面错误";
                    break;
                default:
                    reminder = "界面错误";
                    break;
            }
            //_model.getdata();
        }

        public void select()
        {
            switch (_visibilityform)
            {
                case visibilityform.addressbook:
                    addressbook.select();
                    break;
                case visibilityform.diary:
                    diary.select();
                    break;
                case visibilityform.memorandum:
                    memorandum.select();
                    break;
                case visibilityform.property:
                    property.select();
                    break;
                case visibilityform.reminder:
                    reminder = "界面错误";
                    break;
                default:
                    reminder = "界面错误";
                    break;
            }
        }

        public void modify()
        {
            switch (_visibilityform)
            {
                case visibilityform.addressbook:
                    addressbook.modify();
                    break;
                case visibilityform.diary:
                    diary.modify();
                    break;
                case visibilityform.memorandum:
                    memorandum.modify();
                    break;
                case visibilityform.property:
                    property.modify();
                    break;
                case visibilityform.reminder:
                    reminder = "界面错误";
                    break;
                default:
                    reminder = "界面错误";
                    break;
            }
        }

        public void eliminate()
        {
            _model.getdata();

            switch (_visibilityform)
            {
                case visibilityform.addressbook:
                    addressbook.eliminate();
                    break;
                case visibilityform.diary:
                    diary.eliminate();
                    break;
                case visibilityform.memorandum:
                    memorandum.eliminate();
                    break;
                case visibilityform.property:
                    property.eliminate();
                    break;
                case visibilityform.reminder:
                    reminder = "界面错误";
                    break;
                default:
                    reminder = "界面错误";
                    break;
            }
        }

        public void selectitem(int index)
        {
            //reminder = index.ToString();
        }

        public void selectitem(System.Collections.IList item)
        {
            switch (_visibilityform)
            {
                case visibilityform.addressbook:
                    addressbook.selectitem(item);
                    break;
                case visibilityform.diary:
                    diary.selectitem(item);
                    break;
                case visibilityform.memorandum:
                    memorandum.selectitem(item);
                    break;
                case visibilityform.property:
                    property.selectitem(item);
                    break;
                case visibilityform.reminder:
                    reminder = "selectitem 界面错误";
                    break;
                default:
                    reminder = "selectitem 界面错误";
                    break;
            }
        }

        public viewaddressBook addressbook
        {
            set;
            get;
        }

        public viewproperty property
        {
            set;
            get;
        }

        public viewmemorandum memorandum
        {
            set;
            get;
        }

        public viewdiary diary
        {
            set;
            get;
        }

        //private Visibility _vaddressbook = Visibility.Hidden;
        //private Visibility _vproperty = Visibility.Hidden;
        //private Visibility _vmemorandum = Visibility.Hidden;
        //private Visibility _vdiary = Visibility.Hidden;
        private Visibility _vreminde = Visibility.Visible;
        private visibilityform _visibilityform;
        private void _model_PropertyChanged(object sender , System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(reminder))
            {
                OnPropertyChanged("reminder");
            }
        }
    }

    public enum visibilityform
    {
        addressbook,
        property,
        memorandum,
        diary,
        reminder
    }
}
