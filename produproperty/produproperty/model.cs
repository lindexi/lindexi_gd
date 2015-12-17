using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace produproperty
{
    class model
    {
        public model()
        {

        }

        public string property(string str , bool firstget , bool updateproper)
        {          
            int i = 0;
            i = str.IndexOf("\r\n");
            while (i != -1)
            {
                stringproperty(str.Substring(0 , i) , firstget , updateproper);
               str= str.Substring(i + 2);
                i = str.IndexOf("\r\n");
            }

            stringproperty(str , firstget , updateproper);


            StringBuilder s = new StringBuilder();
            foreach (var temp in publicproperty)
            {
                s.Append(temp + "\r\n");
            }
            foreach (var temp in privatep)
            {
                s.Append(temp + "\r\n");
            }
            return s.ToString();
        }

        private List<string> publicproperty=new List<string>();
        private List<string> privatep=new List<string>();

        private string stringproperty(string str , bool firstget , bool updateproper)
        {
            string a;
            string b;
            string c;
            string s;
            string g;
            a = string.Empty;
            b = string.Empty;
            c = string.Empty;
            s = string.Empty;
            g = string.Empty;

            stringproperty(str , ref a , ref b , ref c);

            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return string.Empty;
            }
            if (updateproper)
            {
                s = "            set\n            { \n                UpdateProper(ref _" + b+ " , value);\n            }";
            }
            else
            {
                s = "            set\n            { \n                _" + b + " = value;\n                OnPropertyChanged(\"" + b + "\");\n            }";
            }


            g = "            get\n            {\n                return _" + b + ";\n            }";

            if (!firstget)
            {
                publicproperty.Add("        public " + a + " " + b + " \n        {\n" + s + "\n" + g + "\n        }");
            }
            else
            {
                publicproperty.Add("        public " + a + " " + b + " \n        {\n" + g + "\n" + s + "\n        }");
            }

    
            privatep.Add("        private " + a+" _"+b+" "+c+"");

            return a + b + c;
        }
     

        private string stringproperty(string str , ref string a , ref string b , ref string c)
        {
            a = null;
            b = null;
            c = null;
            if (string.IsNullOrWhiteSpace(str))
            {
                return string.Empty;
            }
            int i = 0;
            str = str.Trim();
            i = str.IndexOf(' ');
            if (i == -1)
            {
                return string.Empty;
            }
            a = str.Substring(0 , i);
            str = str.Substring(i).Trim();
            i = str.IndexOf('=');
            if (i == -1)
            {
                b = str;
                return str;
            }
            b = str.Substring(0 , i);
            str = str.Substring(i).Trim();
            c = str;
            if (string.Equals(c[c.Length - 1] , ';'))
            {
                return str;
            }
            c += ';';
            return str;
        }
    }
}
