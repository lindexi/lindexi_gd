using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections.Specialized;
using ScrimpNet;
namespace ScrimpNet.Web
{
    public class WebRequest
    {
        public NameValueCollection QueryString { get; set; }
        public NameValueCollection Form { get; set; }
        public NameValueCollection ServerVariables { get; set; }
        public NameValueCollection Params { get; set; }
        public NameValueCollection Headers { get; set; }

        public WebRequest(System.Web.HttpRequest request)
        {
            Headers = request.Headers.Clone();
            Params = request.Params.Clone();
            QueryString = request.QueryString.Clone();
            ServerVariables = request.ServerVariables.Clone();
            Form = request.Form.Clone();
        }

        public static WebRequest New(HttpRequest request)
        {
            return new WebRequest(request);
        }
    }
}
