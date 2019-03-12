using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace md5_加密
{
    class Program
    {
        static void Main(string[] args)
        {
            
        }
        private static string n_md5(string key)
        {            
            string temp;
            int i;
            int str_0_length;
            if(string.IsNullOrEmpty(key))
            {
                temp = "";
                return temp.PadRight(32 , '0');
            }
            str_0_length = Convert.ToInt32(key[0]);
            temp = get_MD5(key);
            for (i = 1; i < str_0_length; i++)
            {
                temp = get_MD5(temp);
            }
            return temp;
        }
        private static string get_MD5(string str)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] temp;
            StringBuilder strb = new StringBuilder();
            temp = md5.ComputeHash(Encoding.Unicode.GetBytes(str));
            md5.Clear();
            for (int i = 0; i < temp.Length; i++)
            {
                strb.Append( temp[i].ToString("X").PadLeft(2 , '0'));
            }
            return strb.ToString().ToLower();
            //return sTemp.ToLower();
        }
    }
}
