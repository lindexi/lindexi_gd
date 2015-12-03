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

            //lajiproperty();
            //lajimemorandum();
            //lajidiary();
            //string strsql = $"{usesql} SELECT ID FROM CONTACTS WHERE NAME='A';";
            //string id = write(strsql);

            //cmemorandum memorandum = new cmemorandum()
            //{
            //    incident="跑内环",
            //    CONTACTSID="华艺"
            //};
            //addmemorandum(memorandum);
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

        public ObservableCollection<cdiary> newdiary()
        {
            const string DIARY = "diary";
            const string contacts = "CONTACTS";
            string strsql;
            ObservableCollection<cdiary> diary = new ObservableCollection<cdiary>();
            //update {DIARY} set CONTACTSID=100 where contactsid is null; {go}
            strsql = $"{usesql}{line} SELECT [{DIARY}].[id],[{DIARY}].[MTIME],[{DIARY}].[PLACE],[{DIARY}].[INCIDENT],[{contacts}].NAME FROM [dbo].[{DIARY}],CONTACTS where {DIARY}.CONTACTSID={contacts}.ID;";
            //错
            const string id = "id";
            const string MTIME = "MTIME";
            const string PLACE = "PLACE";
            const string INCIDENT = "INCIDENT";
            const string CONTACTSID = "NAME";
            using (SqlConnection sql = new SqlConnection(connect))
            {
                sql.Open();
                using (SqlCommand cmd = new SqlCommand(strsql , sql))
                {
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        if (!read.HasRows)
                            return diary;
                        //int idindex = read.GetOrdinal(id);
                        //int MTIMEindex = read.GetOrdinal(MTIME);
                        //int PLACEindex = read.GetOrdinal(PLACE);
                        //int INCIDENTindex = read.GetOrdinal(INCIDENT);
                        //int CONTACTSIDindex = read.GetOrdinal(CONTACTSID);
                        while (read.Read())
                        {
                            diary.Add(new cdiary()
                            {
                                id = DBNullstring<int>(read[id]) ,
                                MTIME = DBNullstring<DateTime>(read[MTIME]) ,
                                PLACE = DBNullstring<string>(read[PLACE]),
                                incident=DBNullstring<string>(read[INCIDENT]),
                                CONTACTSID=DBNullstring<string>(read[CONTACTSID])
                            });
                            //diary.Add(new cdiary()
                            //{    
                            //    id = read.GetInt32(idindex).ToString() ,
                            //    MTIME = read.GetDateTime(MTIMEindex).ToString() ,
                            //    PLACE = read.GetString(PLACEindex) ,
                            //    incident = read.GetString(INCIDENTindex) ,
                            //    CONTACTSID = read.GetString(CONTACTSIDindex)
                            //});
                        }
                    }
                }
            }
            return diary;
        }

        public ObservableCollection<cmemorandum> newmemorandum()
        {
            const string MEMORANDUM = "MEMORANDUM";
            const string contacts = "CONTACTS";
            string strsql;
            ObservableCollection<cmemorandum> memorandum = new ObservableCollection<cmemorandum>();
            //update {MEMORANDUM} set CONTACTSID=100 where CONTACTSID is null;{go}
            strsql = $"{usesql}{line} SELECT [{MEMORANDUM}].[id],[{MEMORANDUM}].[MTIME],[{MEMORANDUM}].[PLACE],[{MEMORANDUM}].[INCIDENT],[{contacts}].NAME FROM [dbo].[{MEMORANDUM}],CONTACTS where {MEMORANDUM}.CONTACTSID={contacts}.ID;";
            //错
            string id = "id";
            string MTIME = "MTIME";
            string PLACE = "PLACE";
            string INCIDENT = "INCIDENT";
            string CONTACTSID = "NAME";
            using (SqlConnection sql = new SqlConnection(connect))
            {
                sql.Open();
                using (SqlCommand cmd = new SqlCommand(strsql , sql))
                {
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        if (!read.HasRows)
                            return memorandum;
                        int idindex = read.GetOrdinal(id);
                        int MTIMEindex = read.GetOrdinal(MTIME);
                        int PLACEindex = read.GetOrdinal(PLACE);
                        int INCIDENTindex = read.GetOrdinal(INCIDENT);
                        int CONTACTSIDindex = read.GetOrdinal(CONTACTSID);

                        while (read.Read())
                        {
                            //if (memorandum == null)
                            //{
                            //    memorandum = new ObservableCollection<cmemorandum>();
                            //}
                            id = read.GetInt32(idindex).ToString();
                            MTIME = DBNullstring<DateTime>(read[MTIMEindex]); //== System.DBNull.Value ? string.Empty : read.GetDateTime(MTIMEindex).ToString();
                            //MTIME = read.GetDateTime(MTIMEindex).ToString();
                            //PLACE = read.GetString(PLACEindex);
                            PLACE = DBNullstring<string>(read[PLACEindex]);
                            INCIDENT = read[INCIDENTindex] as string;
                            CONTACTSID = read[CONTACTSIDindex] as string;

                            memorandum.Add(new cmemorandum()
                            {
                                //id = read.GetInt32(idindex).ToString() ,
                                //MTIME = read.GetDateTime(MTIMEindex).ToString() ,
                                //PLACE = read.GetString(PLACEindex) ,
                                //incident = read.GetString(INCIDENTindex) ,
                                //CONTACTSID = read.GetString(CONTACTSIDindex)
                                id = id ,
                                MTIME = MTIME,//.Trim() ,
                                PLACE = PLACE,//.Trim() ,
                                incident = INCIDENT,//.Trim() ,
                                CONTACTSID = CONTACTSID//.Trim()
                            });
                        }
                    }
                }
            }
            return memorandum;
        }

        public ObservableCollection<cproperty> newproperty()
        {
            const string PROPERTY = "property";
            const string contacts = "CONTACTS";
            /*
SELECT [property].[id]
      ,[terminal]
      ,[PMONEY]
      ,[MTIME]
      ,CONTACTS.NAME AS NAME
  FROM [dbo].[property],CONTACTS
  WHERE [property].CONTACTSID=CONTACTS.ID

UNION

SELECT [property].[id]
      ,[terminal]
      ,[PMONEY]
      ,[MTIME]
      ,NULL AS NAME
  FROM [dbo].[property]
  WHERE [property].CONTACTSID IS NULL;
*/
            string strsql = $"{usesql}{line} SELECT [{PROPERTY}].[id],[terminal],[PMONEY],[MTIME],{contacts}.NAME AS NAME FROM [dbo].[{PROPERTY}],CONTACTS  WHERE [{PROPERTY}].CONTACTSID={contacts}.ID{line}UNION{line} SELECT [{PROPERTY}].[id],[terminal],[PMONEY],[MTIME],NULL AS NAME  FROM [{PROPERTY}] WHERE [{PROPERTY}].CONTACTSID IS NULL;";
            ObservableCollection<cproperty> property = new ObservableCollection<cproperty>();            
            using (SqlConnection sql = new SqlConnection(connect))
            {
                sql.Open();
                using (SqlCommand cmd = new SqlCommand(strsql , sql))
                {
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        if (!read.HasRows)
                            return property;
                        while (read.Read())
                        {
                            property.Add(new cproperty()
                            {
                                id = DBNullstring<int>(read["id"]) ,
                                terminal = DBNullstring<string>(read["terminal"]) ,
                                PMONEY = DBNullstring<decimal>(read["PMONEY"]),
                                MTIME=DBNullstring<DateTime>(read["MTIME"]),
                                CONTACTSID=DBNullstring<string>(read["NAME"])
                            });
                        }
                    }
                }
            }
            return property;
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
            System.Threading.Thread.Sleep(1000);
            ObservableCollection<cdiary> diary = newdiary();
            json = JsonConvert.SerializeObject(diary);
            transmitter = new ctransmitter(-1 , ecommand.diary , json);
            _principal_computer.send(transmitter.ToString());
            System.Threading.Thread.Sleep(1000);
            ObservableCollection<cmemorandum> memorandum = newmemorandum();
            json = JsonConvert.SerializeObject(memorandum);
            transmitter = new ctransmitter(-1 , ecommand.memorandum , json);
            _principal_computer.send(transmitter.ToString());
            System.Threading.Thread.Sleep(1000);
            ObservableCollection<cproperty> property = newproperty();
            json = JsonConvert.SerializeObject(property);
            transmitter = new ctransmitter(-1 , ecommand.property , json);
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
            //string id;
            const string addressBook = "addressbook";
            //const string contacts = "CONTACTS";
            //strsql = $"{usesql}{line}SELECT CONTACTSID{line}FROM {addressBook}{line}WHERE ID='{addressbook.id}';";
            //id = write(strsql);

            //DELETE FROM ADDRESSBOOK
            //WHERE addressBook.ID = '213';
            strsql = $"{usesql}{line}DELETE FROM {addressBook}{line}WHERE {addressBook}.ID = '{addressbook.id}';";
            write(strsql);

            //DELETE FROM CONTACTS
            //WHERE CONTACTS.ID = '218';
            //strsql = $"{usesql}{line}DELETE FROM {contacts}{line}WHERE {contacts}.ID = '{id}';";
            //write(strsql);

            reminder = "删除" + addressbook.id + " " + addressbook.name;
        }

        //修改 DataGrid 

        public void adddiary(cdiary diary)
        {
            string strsql;
            const string DIARY = "DIARY";
            const string contacts = "CONTACTS";
            string id;
            if (diary == null)
            {
                reminder = "添加日记，添加的日记空";
                return;
            }
            //if name==null
            //insert name
            strsql = $"{usesql} SELECT ID FROM {contacts} WHERE NAME='{diary.CONTACTSID}';";
            id = write(strsql);
            if (string.IsNullOrEmpty(id))
            {
                strsql = $"{usesql}{line}insert into {contacts}(name){line}values('{diary.CONTACTSID}') SELECT @@IDENTITY AS Id;";
                id = write(strsql);
            }

            strsql = $"{usesql}{line}insert into {DIARY} (Mtime,PLACE,INCIDENT,CONTACTSID){line}values('{diary.MTIME}','{diary.PLACE}','{diary.incident}','{id}');";
            write(strsql);
            //错
        }

        public void addmemorandum(cmemorandum memorandum)
        {
            string strsql;
            const string MEMORANDUM = "MEMORANDUM";
            const string contacts = "CONTACTS";
            string id;
            if (memorandum == null)
            {
                reminder = "添加备忘，添加的备忘空";
                return;
            }

            strsql = $"{usesql}{line} SELECT ID FROM {contacts} WHERE NAME='{memorandum.CONTACTSID}';";
            id = write(strsql);
            if (string.IsNullOrEmpty(id))
            {
                strsql = $"{usesql}{line}insert into {contacts}(name){line}values('{memorandum.CONTACTSID}') SELECT @@IDENTITY AS Id;";
                id = write(strsql);
            }

            strsql = $"{usesql}{line}insert into {MEMORANDUM} (Mtime,PLACE,INCIDENT,contactsid){line}values('{memorandum.MTIME}','{memorandum.PLACE}','{memorandum.incident}','{id}');";
            write(strsql);
        }

        public void addproperty(cproperty property)
        {
            string strsql;
            const string PROPERTY = "property";
            const string contacts = "CONTACTS";
            string id;

            if (property == null)
            {
                return;
            }

            strsql = $"{usesql}{line} SELECT ID FROM {contacts} WHERE NAME='{property.CONTACTSID}';";
            id = write(strsql);
            if (string.IsNullOrEmpty(id))
            {
                strsql = $"{usesql}{line}insert into {contacts}(name){line}values('{property.CONTACTSID}') SELECT @@IDENTITY AS Id;";
                id = write(strsql);
            }

            strsql = $"{usesql}{line}insert into {PROPERTY}(PMONEY,MTIME,terminal,[CONTACTSID]) values('{property.PMONEY}','{property.MTIME}','{property.terminal}','{id}')";
            write(strsql);
        }

        public void ddiary(cdiary diary)
        {
            string strsql;
            const string DIARY = "DIARY";
            strsql = $"{usesql}{line}DELETE FROM [dbo].[{DIARY}] WHERE id='{diary.id}';";
            write(strsql);
        }

        public void newdiary(cdiary diary)
        {
            string name = diary.CONTACTSID;

            const string contacts = "CONTACTS";
            string strsql;
            string id;
            
            strsql = $"{usesql} SELECT ID FROM {contacts} WHERE NAME='{name}';";
            id = write(strsql);
            if (string.IsNullOrEmpty(id))
            {
                strsql = $"{usesql}{line}insert into {contacts}(name){line}values('{name}') SELECT @@IDENTITY AS Id;";
                id = write(strsql);
            }

            string DIARY = "diary";

            strsql = $" UPDATE [dbo].[{DIARY}]  SET [MTIME] = '{diary.MTIME}' ,[PLACE] = '{diary.PLACE}' ,[INCIDENT] = '{diary.incident}' ,[CONTACTSID] = '{id}' WHERE id='{diary.id}'";

            write(strsql);
        }

        public void dmemorandum(cmemorandum memorandum)
        {
            string strsql;
            const string MEMORANDUM = "memorandum";
            strsql = $"{usesql}{line}DELETE FROM [dbo].[{MEMORANDUM}] WHERE id='{memorandum.id}';";
            write(strsql);
        }

        public void newmemorandum(cmemorandum memorandum)
        {
            string name = memorandum.CONTACTSID;

            const string contacts = "CONTACTS";
            string strsql;
            string id;

            strsql = $"{usesql} SELECT ID FROM {contacts} WHERE NAME='{name}';";
            id = write(strsql);
            if (string.IsNullOrEmpty(id))
            {
                strsql = $"{usesql}{line}insert into {contacts}(name){line}values('{name}') SELECT @@IDENTITY AS Id;";
                id = write(strsql);
            }

            string MEMORANDUM = "memorandum";

            strsql = $" UPDATE [dbo].[{MEMORANDUM}]  SET [MTIME] = '{memorandum.MTIME}' ,[PLACE] = '{memorandum.PLACE}' ,[INCIDENT] = '{memorandum.incident}' ,[CONTACTSID] = '{id}' WHERE id='{memorandum.id}'";
            write(strsql);
        }

        public void dproperty(cproperty property)
        {
            string strsql;
            const string PROPERTY = "property";
            strsql = $"{usesql}{line}DELETE FROM [dbo].[{PROPERTY}] WHERE id='{property.id}';";
            write(strsql);
        }

        public void newproperty(cproperty property)
        {
            string name = property.CONTACTSID;

            const string contacts = "CONTACTS";
            string strsql;
            string id;

            strsql = $"{usesql} SELECT ID FROM {contacts} WHERE NAME='{name}';";
            id = write(strsql);
            if (string.IsNullOrEmpty(id))
            {
                strsql = $"{usesql}{line}insert into {contacts}(name){line}values('{name}') SELECT @@IDENTITY AS Id;";
                id = write(strsql);
            }

            string PROPERTY = "property";

            strsql = $" UPDATE [dbo].[{PROPERTY}]  SET [terminal] ='{property.terminal}' ,[PMONEY] = '{property.PMONEY}',[MTIME] ='{property.MTIME}',[CONTACTSID] = '{id}' WHERE id='{property.id}'";
            write(strsql);
        }

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
                            try
                            {
                                return DBNullstring<int>(read["id"]);
                            }
                            catch
                            {
                                
                            }
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
        private Random ran
        {
            set;
            get;
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
            List<cdiary> diary = new List<cdiary>();
            List<string> temp = new List<string>();
            temp.AddRange(sql.事件.Split(new char[2] { '\r' , '\n' }));
            for (int i = 0; i < temp.Count; i++)
            {
                if (string.IsNullOrEmpty(temp[i]))
                {
                    temp.RemoveAt(i);
                    i--;
                }
                else
                {
                    temp[i] = temp[i].Trim();
                }
            }

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

            int n = 10;
            DateTime time = new DateTime(year: 2012 , month: 1 , day: 1 , hour: 0 , second: 0 , minute: 0);

            for (int i = 0; i < n; i++)
            {
                time = time.AddDays(ran.Next() % 10);
                diary.Add(new cdiary()
                {
                    MTIME = time.ToString() ,
                    PLACE = chinacity[ran.Next(chinacity.Count)] ,
                    incident = temp[ran.Next(temp.Count)] ,
                    CONTACTSID = ran.Next(20 , 201).ToString()
                });
            }

            foreach (var t in diary)
            {
                adddiary(t);
            }
        }

        private void lajimemorandum()
        {
            List<cmemorandum> memorandum = new List<cmemorandum>();
            List<string> temp = new List<string>();
            temp.AddRange(sql.incident.Split(new char[2] { '\r' , '\n' }));
            for (int i = 0; i < temp.Count; i++)
            {
                if (string.IsNullOrEmpty(temp[i]))
                {
                    temp.RemoveAt(i);
                    i--;
                }
                else
                {
                    temp[i] = temp[i].Trim();
                }
            }

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


            DateTime time = new DateTime(year: 2012 , month: 1 , day: 1 , hour: 0 , second: 0 , minute: 0);

            for (int i = 0; true; i++)
            {
                time = time.AddDays(ran.Next() % 10);
                memorandum.Add(new cmemorandum()
                {
                    MTIME = time.ToString() ,
                    //PLACE = chinacity[ran.Next(chinacity.Count)] ,
                    incident = temp[i]
                    //CONTACTSID = ran.Next(20 , 201).ToString()
                });
                if (i == temp.Count - 1)
                {
                    break;
                }
            }

            foreach (var t in memorandum)
            {
                addmemorandum(t);
            }
        }

        private void lajiproperty()
        {
            DateTime time = new DateTime(year: 2012 , month: 1 , day: 1 , hour: 0 , second: 0 , minute: 0);
            List<cproperty> property = new List<cproperty>();
            int n = 100;
            int money;
            for (int i = 0; i < n; i++)
            {
                time = time.AddDays(ran.Next() % 10);
                if (ran.Next() % 3 == 1)
                {
                    money = -1;
                }
                else
                {
                    money = 1;
                }
                property.Add(new cproperty()
                {
                    MTIME = time.ToString() ,
                    PMONEY = ( money * ran.Next(10 , 100) ).ToString()
                });
            }

            foreach (var t in property)
            {
                addproperty(t);
            }
        }

        private void implement(int id , ecommand command , string str)
        {
            caddressBook addressbook;
            cdiary diary;
            cmemorandum memorandum;
            cproperty property;
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
                case ecommand.adddiary:
                    diary = Deserialize<cdiary>(str);
                    adddiary(diary);
                    break;
                case ecommand.ddiary:
                    diary = Deserialize<cdiary>(str);
                    ddiary(diary);
                    break;
                case ecommand.newdiary:
                    diary= Deserialize<cdiary>(str);
                    newdiary(diary);
                    break;
                case ecommand.addmemorandum:
                    memorandum = Deserialize<cmemorandum>(str);
                    addmemorandum(memorandum);
                    break;
                case ecommand.dmemorandum:
                    memorandum = Deserialize<cmemorandum>(str);
                    dmemorandum(memorandum);
                    break;
                case ecommand.newmemorandum:
                    memorandum = Deserialize<cmemorandum>(str);
                    newmemorandum(memorandum);
                    break;
                case ecommand.addproperty:
                    property = Deserialize<cproperty>(str);
                    addproperty(property);
                    break;
                case ecommand.dproperty:
                    property = Deserialize<cproperty>(str);
                    dproperty(property);
                    break;
                case ecommand.newproperty:
                    property = Deserialize<cproperty>(str);
                    newproperty(property);
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
                reminder = "输入不是ObservableCollection<T> json" + e.Message;
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

        private string DBNullstring<T>(object obj)
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
