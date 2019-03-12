using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 人工智能
{
    class Program
    {
        static void Main(string[] args)
        {
            垃圾 a=new 垃圾();
        }

        public class 垃圾
        {
            public 垃圾()
            {
                做_buffer_string();
                laji();
                //StringBuilder str= new StringBuilder();
                //int i;
                //foreach (string yi in buffer)
                //{
                //    str.Append("private int " + yi + "(int k)\n{\nif(k>10000)\n{\nreturn k;\n}\nswitch (k)\n{\n");
                //    i = 0;
                //    foreach (string t in buffer)
                //    {
                //        str.Append("case " + i.ToString() + ":k=" + buffer[_ran.Next() % buffer.Count] + "(k+" + _ran.Next(1000).ToString() + ");break;\n");
                //        //int i;
                //        //switch i
                //        //{
                //        //case :;break;
                //        //}
                //        i++;
                //    }
                //    str.Append("}\nreturn k;\n}\n");
                //}
                //写文件(str.ToString());
            }

            private List<string> buffer;
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
                关();
                int 删;
                删 = buffer.Count;
                //字符串数 = _ran.Next(1000); //+ 100;
                字符串数 = 100;
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
                    else
                    {
                        i--;
                    }
                }
                str.Clear();
                buffer.RemoveRange(0 , 删);
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
            private string using_namespace()
            {
                StringBuilder str=new StringBuilder();
                str.Append("using System;\nusing System.Collections.Generic;\nusing System.IO;\nusing System.Linq;\nusing System.Text;\nusing System.Threading.Tasks;\nusing System.Windows;\nusing System.Windows.Controls;\nusing System.Windows.Data;\nusing System.Windows.Documents;\nusing System.Windows.Input;\nusing System.Windows.Media;\nusing System.Windows.Media.Imaging;\nusing System.Windows.Navigation;\nusing System.Windows.Shapes;\n");
                return str.ToString();
            }
            private string name_space()
            {
                string temp="人工智能";
                return "nnamespace " + temp + "\n{\n";
            }

            private string laji_class()
            {
                string _class=buffer[0];
                StringBuilder str=new StringBuilder();
                buffer.RemoveAt(0);
                int i;
                string _public= "        public class " + _class + @"\n        {\n            public " + _class + @"()
            {" + "\n";
                str.Append(_public);
                for (i = 0; i < buffer.Count; i++)
                {
                    str.Append("                _" + buffer[i] + "_int=0;\n");
                    //str.Append("                _" + buffer[i] + "_bool=false;\n");
                }
                for (i = 0; i < buffer.Count; i++)
                {
                    str.Append("                _" + buffer[i] + "_bool=false;\n");
                }
                str.Append("            }");
                return str.ToString();
            }
            private string laji_main()
            {
                string temp = @"            }
            public string main(string str)
            {
                return " + buffer[_ran.Next(buffer.Count)] + @"(str);
            }";
                return temp;
            }

            private string laji_buffer(int i)
            {
                StringBuilder str=new StringBuilder();
                string temp;
                int t;
                int j;
                str.Append(@"
            private string " + buffer[i] + "(string str)\n");
                str.Append(@"            {
                int temp_int;
                string temp_string;
                temp_int = 0;
                temp_string = str;
                if (!_" + buffer[i] + "_bool)\n");
                str.Append(@"                {" + "\n");
                str.Append(@"                    _" + buffer[i] + "_bool = true;\n");
                str.Append(@"
                    try
                    {
                        temp_int = Convert.ToInt32(str);
                    }
                    catch
                    {
                        str = _" + buffer[i] + "_int.ToString();");
                str.Append(@"
                        temp_int = _" + buffer[i] + "_int;" + "\n");
                str.Append(@"                    }" + "\n");
                str.Append(@"                    switch(temp_int)
                    {" + "\n");
                t = 0;
                for (j = 0; j < buffer.Count; j++)
                {
                    t = t + _ran.Next(buffer.Count) + 1;
                    temp = buffer[_ran.Next(buffer.Count)];
                    str.Append("                        case " + t.ToString());
                    str.Append(@":
                            if(!_" + temp + @"_bool)
                            {
                                temp_string = " + temp + @"(temp_int.ToString());
                            }
                            break;" + "\n");
                }
                str.Append("                    }\n");
                str.Append(@"                    try
                    {
                        temp_int = Convert.ToInt32(temp_string); 
                    }
                    catch
                    {
                        
                    }
                    _" + buffer[i] + "_int = temp_int;");
                if (i < buffer.Count - 1)
                {
                    str.Append(@"
                    return " + buffer[i + 1] + "(_" + buffer[i] + "_int.ToString());\n");
                }
                else
                {
                    str.Append(@"
                    return _" + buffer[0] + "_int.ToString();");
                }
                str.Append(@"                }
                else
                {
                    try
                    {
                        temp_int = Convert.ToInt32(str);
                        str = (temp_int + _" + buffer[i] + @"_int).ToString();
                    }
                    catch
                    {
                    
                    }" + "\n");

                str.Append(@"                    return str;
                }
            }");
                return str.ToString();
            }
            private string laji_private()
            {
                StringBuilder str=new StringBuilder();
                int i;
                for (i = 0; i < buffer.Count; i++)
                {
                    str.Append("            private int _" + buffer[i] + "_int;\n");
                }
                for (i = 0; i < buffer.Count; i++)
                {
                    str.Append("            private bool _" + buffer[i] + "_bool;\n");
                }
                return str.ToString();
            }
            private string end()
            {
                string _class="    }\n";
                string _name_space="}";
                return _class + _name_space;
            }
            private void laji()
            {
                StringBuilder str= new StringBuilder();
                string temp;
                int i;
                int j;
                int t;
                str.Append("using System;\nusing System.Collections.Generic;\nusing System.IO;\nusing System.Linq;\nusing System.Text;\nusing System.Threading.Tasks;\nusing System.Windows;\nusing System.Windows.Controls;\nusing System.Windows.Data;\nusing System.Windows.Documents;\nusing System.Windows.Input;\nusing System.Windows.Media;\nusing System.Windows.Media.Imaging;\nusing System.Windows.Navigation;\nusing System.Windows.Shapes;\nnamespace 人工智能\n{");//    public class " + buffer[0] + "{ public " + buffer[0] + "()");// + "{}\npublic int main(int str)\n{\n");
                temp = @"
        public class " + buffer[0] + @"
        {
            public " + buffer[0] + @"()
            {" + "\n";
                str.Append(temp);
                file_address = "Y:\\" + buffer[0] + ".cs";
                buffer.RemoveAt(0);
                for (i = 0; i < buffer.Count; i++)
                {
                    str.Append("                _" + buffer[i] + "_int=0;\n");
                    str.Append("                _" + buffer[i] + "_bool=false;\n");
                }
                temp = @"            }
            public string main(string str)
            {
                return " + buffer[_ran.Next(buffer.Count)] + @"(str);
            }";
                str.Append(temp);
                for (i = 0; i < buffer.Count; i++)
                {
                    str.Append(@"
            private string " + buffer[i] + "(string str)\n");
                    str.Append(@"            {
                int temp_int;
                string temp_string;
                temp_int = 0;
                temp_string = str;
                if (!_" + buffer[i] + "_bool)\n");
                    str.Append(@"                {" + "\n");
                    str.Append(@"                    _" + buffer[i] + "_bool = true;\n");
                    str.Append(@"
                    try
                    {
                        temp_int = Convert.ToInt32(str);
                    }
                    catch
                    {
                        str = _" + buffer[i] + "_int.ToString();");
                    str.Append(@"
                        temp_int = _" + buffer[i] + "_int;" + "\n");
                    str.Append(@"                    }" + "\n");
                    str.Append(@"                    switch(temp_int)
                    {" + "\n");
                    t = 0;
                    for (j = 0; j < buffer.Count; j++)
                    {
                        t = t + _ran.Next(buffer.Count) + 1;
                        temp = buffer[_ran.Next(buffer.Count)];
                        str.Append("                        case " + t.ToString());
                        str.Append(@":
                            if(!_" + temp + @"_bool)
                            {
                                temp_string = " + temp + @"(temp_int.ToString());
                            }
                            break;" + "\n");
                    }
                    str.Append("                    }\n");
                    str.Append(@"                    try
                    {
                        temp_int = Convert.ToInt32(temp_string); 
                    }
                    catch
                    {
                        
                    }
                    _" + buffer[i] + "_int = temp_int;");
                    if (i < buffer.Count - 1)
                    {
                        str.Append(@"
                    return " + buffer[i + 1] + "(_" + buffer[i] + "_int.ToString());\n");
                    }
                    else
                    {
                        str.Append(@"
                    return _" + buffer[0] + "_int.ToString();");
                    }
                    str.Append(@"                }
                else
                {
                    try
                    {
                        temp_int = Convert.ToInt32(str);
                        str = (temp_int + _" + buffer[i] + @"_int).ToString();
                    }
                    catch
                    {
                    
                    }" + "\n");

                    str.Append(@"                    return str;
                }
            }");
                }

                for (i = 0; i < buffer.Count; i++)
                {
                    str.Append("            private int _" + buffer[i] + "_int;\n");
                    str.Append("            private bool _" + buffer[i] + "_bool;\n");
                }
                str.Append("}        " + "\n");




                //for (i = 0; i < buffer.Count; i++)
                //{
                //    str.Append("_" + buffer[i] + "_used=false;\n");
                //    str.Append("str=str+" + buffer[i] + "(str);\n");
                //}
                //str.Append("return  " + buffer[0] + "(str);\n}");
                //for (i = 0; i < buffer.Count; i++)
                //{
                //    //str.Append("private" + " int " + buffer[i] + "(int str)\n{\nif(str>10000)\n{\nreturn str;\n}\nswitch (str)\n{\n");
                //    str.Append("private " + "bool _" + buffer[i] + "_used;");
                //    str.Append("private" + " int " + buffer[i] + "(int str)\n{str++;if(_" + buffer[i] + "_used){return str++;}\nelse\n{\n_" + buffer[i] + "_used=true;\n}\nswitch (str)\n{\n");
                //    j = 0;
                //    for (j = 0; j < buffer.Count; j++)
                //    {
                //        str.Append("case " + j.ToString() + ":str=" + buffer[_ran.Next() % buffer.Count] + "(str+" + _ran.Next(buffer.Count).ToString() + ");break;\n");
                //    }
                //    str.Append("}\n");
                //    //if (i + 1 != buffer.Count)
                //    //{
                //    //    str.Append("return str+"+buffer[i+1]+"(str);\n}\n");
                //    //}
                //    //else
                //    //{
                //    //    str.Append("return str;\n}\n");
                //    //}
                //    str.Append("return str;\n}\n");
                //}

                str.Append("}");
                写文件(str.ToString());
                /*.Replace("\n","\r\n")*/
            }
            private void 关()
            {
                buffer.Add("abstract");
                buffer.Add("as");
                buffer.Add("base");
                buffer.Add("bool");
                buffer.Add("break");
                buffer.Add("byte");
                buffer.Add("case");
                buffer.Add("catch");
                buffer.Add("char");
                buffer.Add("Checked");
                buffer.Add("class");
                buffer.Add("const");
                buffer.Add("continue");
                buffer.Add("decimal");
                buffer.Add("default");
                buffer.Add("delegate");
                buffer.Add("do");
                buffer.Add("double");
                buffer.Add("else");
                buffer.Add("enum");
                buffer.Add("event");
                buffer.Add("explicit");
                buffer.Add("extern");
                buffer.Add("false");
                buffer.Add("finally");
                buffer.Add("fixed");
                buffer.Add("float");
                buffer.Add("for");
                buffer.Add("foreach");
                buffer.Add("goto");
                buffer.Add("if");
                buffer.Add("implicit");
                buffer.Add("in");
                buffer.Add("int");
                buffer.Add("interface");
                buffer.Add("internal");
                buffer.Add("is");
                buffer.Add("lock");
                buffer.Add("long");
                buffer.Add("namespace");
                buffer.Add("new");
                buffer.Add("null");
                buffer.Add("Object");
                buffer.Add("operator");
                buffer.Add("out");
                buffer.Add("override");
                buffer.Add("params");
                buffer.Add("private");
                buffer.Add("protected");
                buffer.Add("public");
                buffer.Add("readonly");
                buffer.Add("ref");
                buffer.Add("return");
                buffer.Add("sbyte");
                buffer.Add("sealed");
                buffer.Add("short");
                buffer.Add("sizeof");
                buffer.Add("stackalloc");
                buffer.Add("static");
                buffer.Add("string");
                buffer.Add("struct");
                buffer.Add("switch");
                buffer.Add("this");
                buffer.Add("throw");
                buffer.Add("true");
                buffer.Add("try");
                buffer.Add("typeof");
                buffer.Add("uint");
                buffer.Add("ulong");
                buffer.Add("Unchecked");
                buffer.Add("unsafe");
                buffer.Add("ushort");
                buffer.Add("using");
                buffer.Add("virtual");
                buffer.Add("void");
                buffer.Add("volatile");
                buffer.Add("while");
                buffer.Add("add");
                buffer.Add("alias");
                buffer.Add("ascending");
                buffer.Add("async");
                buffer.Add("await");
                buffer.Add("descending");
                buffer.Add("dynamic");
                buffer.Add("源");
                buffer.Add("get");
                buffer.Add("global");
                buffer.Add("group");
                buffer.Add("into");
                buffer.Add("join");
                buffer.Add("let");
                buffer.Add("orderby");
                buffer.Add("partial");
                buffer.Add("remove");
                buffer.Add("select");
                buffer.Add("set");
                buffer.Add("value");
                buffer.Add("var");
                buffer.Add("where");
                buffer.Add("yield");
            }
            private string file_address;
            private Random _ran=new Random();
        }

        public class laji
        {
            public laji()
            {
                _l1_int = 0;
                _l1_bool = false;
            }
            public string main(string str)
            {
                return l1(str);
            }
            private int _l1_int;
            private bool _l1_bool;
            private string l1(string str)
            {
                int temp_int;
                string temp_string;
                temp_int = 0;
                temp_string = str;
                if (!_l1_bool)
                {
                    try
                    {
                        temp_int = Convert.ToInt32(str);
                    }
                    catch
                    {
                        str = _l1_int.ToString();
                        temp_int = _l1_int;
                    }
                    switch (temp_int)
                    {
                        case 0:
                            if (_l1_bool == false)
                            {
                                temp_string = l2(temp_int.ToString());
                            }
                            break;
                    }
                    try
                    {
                        temp_int = Convert.ToInt32(temp_string);
                    }
                    catch
                    {

                    }
                    _l1_int = temp_int;
                    _l1_bool = true;
                    return l2(_l1_int.ToString());
                }
                else
                {
                    try
                    {
                        temp_int = Convert.ToInt32(str);
                        str = (temp_int + _l1_int).ToString();
                    }
                    catch
                    {

                    }
                    return str;
                }
            }
            private string l2(string str)
            {
                return str;
            }
        }
    }
}
