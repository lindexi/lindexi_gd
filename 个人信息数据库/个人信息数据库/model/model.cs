using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
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

        //public void ce()
        {
            string inputJsonString = @"
                [
                    {StudentID:'100',Name:'aaa',Hometown:'china'},
                    {StudentID:'101',Name:'bbb',Hometown:'us'},
                    {StudentID:'102',Name:'ccc',Hometown:'england'}
                ]";
            JArray jsonObj = JArray.Parse(inputJsonString);
            string message = @"<table border='1'>
                    <tr><td width='80'>StudentID</td><td width='100'>Name</td><td width='100'>Hometown</td></tr>";
            string tpl = "<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>";
            foreach (JObject jObject in jsonObj)
            {
                message += String.Format(tpl , jObject["StudentID"] , jObject["Name"] , jObject["Hometown"]);
            }
            message += "</table>";
            //lbMsg.InnerHtml = message;
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
