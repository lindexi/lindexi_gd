using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace 汉字转拼音
{
    public class pinyin  
    {  
        string url = "http://apis.baidu.com/xiaogg/changetopinyin/topinyin";
        string param = "str=%E7%99%BE%E5%BA%A6&type=json&traditional=0&accent=0&letter=0&only_chinese=0";
        //string result = request(url , param);

        /// <summary>
        /// 发送HTTP请求
        /// </summary>
        /// <param name="url">请求的URL</param>
        /// <param name="param">请求的参数</param>
        /// <returns>请求结果</returns>
        public string request(string url , string param)
        {
            string strURL = url + '?' + param;
            System.Net.HttpWebRequest request;
            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
            request.Method = "GET";
            // 添加header
            request.Headers.Add("apikey" , "您自己的apikey");
            System.Net.HttpWebResponse response;
            response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream s;
            s = response.GetResponseStream();
            string StrDate = "";
            string strValue = "";
            StreamReader Reader = new StreamReader(s , Encoding.UTF8);
            while (( StrDate = Reader.ReadLine() ) != null)
            {
                strValue += StrDate + "\r\n";
            }
            return strValue;
        }
    }
}
