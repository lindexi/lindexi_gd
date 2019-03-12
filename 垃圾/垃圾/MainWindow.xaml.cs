using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 垃圾
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            qbopqylcuaijsomyvnjdalfclbyy _qbopqylcuaijsomyvnjdalfclbyy =new qbopqylcuaijsomyvnjdalfclbyy();
            _qbopqylcuaijsomyvnjdalfclbyy.main(0);
      
            file_address = "Y:\\data.cs";
        }
        
        private void btn_Click(object sender , RoutedEventArgs e)
        {
            //bmhjmqanfpkbozgexxaptoeyqjw _bmhjmqanfpkbozgexxaptoeyqjw=new bmhjmqanfpkbozgexxaptoeyqjw();
            int i;
            for (i = 0; i < 1; i++)
            {
                //xt.Text += _bmhjmqanfpkbozgexxaptoeyqjw.main(i) + " ";
            }

            做_buffer_string();
            //垃圾();
            laji();
        }

        List<string> buffer;
        private string 随机字符串()
        {
            //65 90  97 122 57
            StringBuilder str=new StringBuilder();
            int 长;
            //bool 完成;
            //List<string> key=new List<string>();
            //if(_ran.Next()%2==1)
            //{
            //    str.Append(Convert.ToChar(_ran.Next(65 , 91)));
            //}
            //else
            //{
            //    str.Append(Convert.ToChar(_ran.Next(97 , 123)));
            //}
            长 = _ran.Next(10) + 20;
            //完成 = true;
            //key.Add("str");
            //key.Add("int");
            //key.Add("long");
            //key.Add("public");
            //key.Add("private");
            //while (完成)
            // {
            for (; 长 > 0; 长--)
            {
                str.Append(Convert.ToChar(_ran.Next(97 , 123)));
            }
            // }
            if (长 == 0)
            {
                return str.ToString();
            }
            长 = _ran.Next(10) + 1;

            for (; 长 > 0; 长--)
            {
                int t=_ran.Next() % 3;
                if (t == 2)
                {
                    str.Append(Convert.ToChar(_ran.Next(65 , 91)));
                }
                else if (t == 1)
                {
                    str.Append(Convert.ToChar(_ran.Next(48 , 58)));
                }
                else
                {
                    str.Append(Convert.ToChar(_ran.Next(97 , 123)));
                }

            }
            return str.ToString();
        }
        private void 做_buffer_string()
        {
            buffer = new List<string>();
            string temp;
            int 字符串数;
            int i;
            bool repeat;//重复            
            StringBuilder str=new StringBuilder();
            字符串数 = _ran.Next(1000) + 100;
            for (i = 0; i < 字符串数; i++)
            {
                temp = 随机字符串();
                repeat = false;
                foreach (var tmp in buffer)
                {
                    if (temp == tmp)
                    {
                        repeat = true;
                        break;
                    }
                }
                if (!repeat)
                {
                    buffer.Add(temp);
                }
            }
            str.Clear();
            //foreach (string t in buffer)
            //{
            //    str.Append(t);
            //}
            //xt.Text = str.ToString();
            //写文件(str.ToString());

        }
        private void 写文件(string str)
        {
            FileStream file = new FileStream(file_address , FileMode.Create);
            byte[] buf;
            //foreach (string temp in decryption)
            // {
            buf = Encoding.UTF8.GetBytes(str);
            file.Write(buf , 0 , buf.Length);
            //}
            //decryption.Clear();
            file.Flush();
            file.Close();
            //return true;
        }
        private void laji()
        {
            StringBuilder str= new StringBuilder();
            int i;
            int j;
            str.Append("using System;\nusing System.Collections.Generic;\nusing System.IO;\nusing System.Linq;\nusing System.Text;\nusing System.Threading.Tasks;\nusing System.Windows;\nusing System.Windows.Controls;\nusing System.Windows.Data;\nusing System.Windows.Documents;\nusing System.Windows.Input;\nusing System.Windows.Media;\nusing System.Windows.Media.Imaging;\nusing System.Windows.Navigation;\nusing System.Windows.Shapes;\nnamespace 人工智能\n{\n    public class " + buffer[0] + "{ public " + buffer[0] + "()" + "{}\npublic int main(int str)\n{\n");
            file_address = "Y:\\" + buffer[0] + ".cs";
            buffer.RemoveAt(0);
            for (i = 0; i < buffer.Count; i++)
            {
                str.Append("_" + buffer[i] + "_used=false;\n");
                str.Append("str=str+" + buffer[i] + "(str);\n");
            }
            str.Append("return  "+buffer[0] + "(str);\n}");
            for (i = 0; i < buffer.Count; i++)
            {
                //str.Append("private" + " int " + buffer[i] + "(int str)\n{\nif(str>10000)\n{\nreturn str;\n}\nswitch (str)\n{\n");
                str.Append("private " + "bool _" +buffer[i]+ "_used;");
                str.Append("private" + " int " + buffer[i] + "(int str)\n{str++;if(_" + buffer[i] + "_used){return str++;}\nelse\n{\n_" + buffer[i] + "_used=true;\n}\nswitch (str)\n{\n");
                j = 0;
                for (j = 0; j < buffer.Count; j++)
                {
                    str.Append("case " + j.ToString() + ":str=" + buffer[_ran.Next() % buffer.Count] + "(str+" + _ran.Next(buffer.Count).ToString() + ");break;\n");
                }
                str.Append("}\n");
                //if (i + 1 != buffer.Count)
                //{
                //    str.Append("return str+"+buffer[i+1]+"(str);\n}\n");
                //}
                //else
                //{
                //    str.Append("return str;\n}\n");
                //}
                str.Append("return str;\n}\n");
            }
            str.Append("}}");
            写文件(str.ToString());
        }
        private void 垃圾()
        {
            StringBuilder str= new StringBuilder();
            int i;
            foreach (string yi in buffer)
            {
                str.Append("private int " + yi + "(int k)\n{\nif(k>10000)\n{\nreturn k;\n}\nswitch (k)\n{\n");
                i = 0;
                foreach (string t in buffer)
                {
                    str.Append("case " + i.ToString() + ":k=" + buffer[_ran.Next() % buffer.Count] + "(k+" + _ran.Next(1000).ToString() + ");break;\n");
                    //int i;
                    //switch i
                    //{
                    //case :;break;
                    //}
                    i++;
                }
                str.Append("}\nreturn k;\n}\n");
            }
            写文件(str.ToString());
        }
        private string file_address;
        private Random _ran=new Random();
    }
}
