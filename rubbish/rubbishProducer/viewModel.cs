using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ViewModel
{
    public class viewModel : notify_property
    {
        public  viewModel()
        {
            _reminder = new StringBuilder();  
            rubbish();
        }
        public string reminder
        {
            set
            {
                _reminder.Clear();
                _reminder.Append(value);
                OnPropertyChanged("reminder");
            }
            get
            {
                return _reminder.ToString();
            }
        }

        public Random ran
        {
            set
            {
                _ran = value;
            }
            get
            {
                return _ran;
            }
        }

        public async void rubbish()
        {
            //string line = "\r\n";
            string usingstr = string.Format("using System;{0}using System.Collections.Generic;{0}using System.Linq;{0}using System.Text;{0}using System.Threading.Tasks;{0}namespace rubbish{1}" , line , left);
            //string classstr = ranstr();
            int count;
            StringBuilder str = new StringBuilder();

            for (count = 0; count < 100; count++)
            {
                junk.Add(ranstr()+count.ToString());
            }
            str.Append(usingstr);
            str.Append(constructor());
            for (int i = 10; i < junk.Count; i++)
            {
                element.Add(lajicontent(junk[i]));
            }
            element.Add(content1());
            element.Add(content2());
            for (int i = 0; i < element.Count; )
            {
                count = ran.Next() % element.Count;
                str.Append(element[count]);
                element.RemoveAt(count);
            }
            for (int i = 10; i < junk.Count; i++)
            {
                //temp = string.Format("{0}(o);{1}" , junk[i] , line);
                //content.Append(temp);
                str.Append(string.Format("{0}private bool _{1}_bool;{2}" , space , junk[i] , line));
            }
            for (int i = 10; i < junk.Count; i++)
            {
                str.Append(string.Format("{0}private int _{1}_int;{2}" , space , junk[i] , line));
            }            
            str.Append("}\r\n}");           
            StorageFile file =await ApplicationData.Current.LocalFolder.CreateFileAsync(junk[0]+".cs",CreationCollisionOption.ReplaceExisting);
            using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
            {
                using (Windows.Storage.Streams.DataWriter dataWriter = new Windows.Storage.Streams.DataWriter(transaction.Stream))
                {
                    dataWriter.WriteString(str.ToString());
                    transaction.Stream.Size = await dataWriter.StoreAsync();
                    await transaction.CommitAsync();
                }
            }

            junk.Clear();
            str.Clear();
            _reminder.Append(file.Path);
            OnPropertyChanged("reminder");
        }
        private string constructor()
        {
            StringBuilder content = new StringBuilder();

            string space = "        ";
            string temp;
            //public class laji
            //{
            temp = string.Format("public class {0}{1}" , junk[0] , left);
            content.Append(temp);
            //public laji(object o)
            //{
            //    main(o);
            //}
            temp = string.Format("{0}public {1}(object o){2}" , space , junk[0] , left);
            content.Append(temp);
            for (int i = 10; i < junk.Count; i++)
            {
                temp = string.Format("{0}_{1}_bool=false;{2}" , space , junk[i] , line);
                content.Append(temp);
                temp = string.Format("{0}_{1}_int=0;{2}" , space , junk[i] , line);
                content.Append(temp);
            }
            temp = string.Format("    main(o);{0}" , right);
            content.Append(temp);
            //public void main(object o)
            //{
            //    asdofhiuae(o);
            //}            
            temp = string.Format("public void main(object o){0}" , left);
            content.Append(temp);
            for (int i = 10; i < junk.Count; i++)
            {
                temp = string.Format("{0}(o);{1}" , junk[i] , line);
                content.Append(temp);
            }
            temp = string.Format("{0}" , right);
            content.Append(temp);
            return content.ToString();
        }
        private string lajicontent(string str)
        {
            StringBuilder content = new StringBuilder();
            string temp;
            List<string> junkstr = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                junkstr.Add(ranstr());
            }
            //if (_asdofhiuae_bool)
            //{
            //    return ( o as int? ) + _asdofhiuae_int;
            //}
            //0 space 1 str 2 left 3 right
            content.Clear();
            string boolstr = string.Format("{0}if (_{1}_bool){2}    return ( o as int? ) + _{1}_int;{3}" , space , str , left , right);
            content.Append(boolstr);
            //_asdofhiuae_bool = true;
            string truestr = string.Format("{0}_{1}_bool = true;{2}" , space , str , line);
            content.Append(truestr);
            //int n = uh6x9eptld96543audp531onsjf7pz12(o);
            string nstr =junkstr[0];
            temp = string.Format("{0}int {1} = {2}(o);{3}" , space , nstr , junk[1] , line);
            content.Append(temp);
            //if (n == 0)
            //{
            //    n = _asdofhiuae_int;
            //}
            temp = string.Format("{0}if ({1} == 0){2}    {1} = _{3}_int;{4}" , space , nstr , left , str , right);
            content.Append(temp);
            //switch (n)
            //{
            //    default:
            //        break;
            //}
            content.Append(switchcontent(nstr));
            //return n;
            temp = string.Format("{0}return {1};{2}" , space , nstr , line);
            content.Append(temp);
            return laji(str , content.ToString());
        }
        private string switchcontent(string str)
        {
            //switch (n)
            //{
            //    default:
            //        break;
            //}
            StringBuilder content = new StringBuilder();
            string temp;
            int count;
            count = 0;
            temp = string.Format("{0}switch({1}){2}" , space , str , left);
            content.Append(temp);
            for (int i = 0; i < 100; i++)
            {
                //case 1:
                //    if (!_vq0qmhx3k048a92n4sec8lt011s4kqm1_bool)
                //{
                //    o = vq0qmhx3k048a92n4sec8lt011s4kqm1(o);
                //}
                //break;
                temp = string.Format("{0}    if (!_{1}_bool){2}o = {1}(o);{3}" , space,junk[(count%(junk.Count-10))+10],left,right);
                content.Append(string.Format("{0}case {1}:{2}{3}break;{3}" , space , count ,temp , line));
                count += ran.Next(1,100);
            }
            temp = string.Format("{0}default:{1}{0}break;" , space , line);
            content.Append(temp);
            temp = string.Format("{0}" , right);
            content.Append(temp);
            return content.ToString();
        }

        private string laji(string str , string content)
        {
            string temp = string.Format("private object {0}(object o)" , str);
            return temp + "\r\n{\r\n" + content + "\r\n}\r\n";
        }

        private string content1()
        {
            string content = @"            int? n = 0;
            if (o is string)
            {
                n = " + junk[2] + @"(o as string);
            }
            else if (o is int)
            {
                n = o as int?;
            }
            return (int)n;";
            string temp = string.Format("private int {0}(object o)" , junk[1]);
            return temp + "\r\n{\r\n" + content + "\r\n}\r\n";
        }
        private string content2()
        {
            string content = @"            int n = 0;
            int i;
            for (i = 0; i < str.Length; i++)
            {
                if (str[i] <= '9' && str[i] >= '0')
                {

                }
                else
                {
                    break;
                }
            }
            str = str.Substring(0 , i);
            try
            {
                n = Convert.ToInt32(str);
            }
            catch
            {
                n = 0;
            }
            return n;";
            string temp = string.Format("private int {0}(string str)" , junk[2]);
            return temp + "\r\n{\r\n" + content + "\r\n}\r\n";
            //return string.Format("private int {0}(string str){1}{{1}{2}{1}}{1}" , junk[2] , line , content);
        }


        private string ranstr()
        {
            int count = 32;
            bool english = true;
            bool num = true;

            StringBuilder str = _temp;
            if (count < 1)
            {
                return null;
            }
            str.Clear();
            if (english)
            {
                str.Append(ranenglish());
            }
            for (int i = 1; i < count; i++)
            {
                if (num && english)
                {
                    if (ran.Next() % 2 == 0)
                    {
                        str.Append(ranenglish());
                    }
                    else
                    {
                        str.Append(rannum());
                    }
                }
                else if (english && !num)
                {
                    str.Append(ranenglish());
                }
            }
            return str.ToString();
        }
        private char ranenglish()
        {
            return (char)ran.Next((int)'a' , (int)'z' + 1);
        }
        private char rannum()
        {
            return (char)ran.Next((int)'0' , (int)'9' + 1);
        }
        
        private StringBuilder _reminder;
        private Random _ran = new Random();
        private StringBuilder _temp = new StringBuilder();
        private List<string> element = new List<string>();
        private List<string> junk = new List<string>();
        private string line = "\r\n";
        private string space = "            ";
        private string left = "\r\n            {\r\n            ";
        private string right = "\r\n            }\r\n";
    }
}
