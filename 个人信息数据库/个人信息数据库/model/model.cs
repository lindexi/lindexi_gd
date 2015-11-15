using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 个人信息数据库.model
{
    public class model
    {
        public model()
        {
            ran = new Random();
            
        }
        Random ran
        {
            set;
            get;
        }
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
    }
}
