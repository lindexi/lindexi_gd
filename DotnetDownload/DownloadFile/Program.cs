using System;
using System.IO;
using System.Net;

namespace DownloadFile
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = "http://localhost:5000/download";
            var request = (HttpWebRequest)System.Net.HttpWebRequest.Create(url);
            request.AddRange(100, 20000);
            using var ns = request.GetResponse().GetResponseStream();
            var file = "1.txt";
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            using var stream = new FileStream(file, FileMode.Create);
            var buffer = new byte[1024];
            
        }
    }
}
