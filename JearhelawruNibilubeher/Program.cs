using System;
using System.Collections.Specialized;
using System.Web;

namespace JearhelawruNibilubeher
{
    class Program
    {
        static void Main(string[] args)
        {
            var uriBuilder = new UriBuilder(new Uri("http://blog.lindexi.com"));
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["Foo"] = "123";
            query["doubi"] = "doubi";
            uriBuilder.Query = query.ToString();

            Console.WriteLine(uriBuilder.Uri);
        }
    }
}
