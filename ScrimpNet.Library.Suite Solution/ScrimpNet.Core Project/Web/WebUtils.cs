/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Collections.Specialized;
using System.Security.Principal;

namespace ScrimpNet.Web
{
    /// <summary>
    /// Miscellaneous collection of web-centric methods
    /// </summary>
    public static class WebUtils
    {
        public static T Request<T>(string key)
        {

            if (HttpContext.Current == null)
            {
                throw new InvalidOperationException("This method may only be executed within the context of an HTTP context");
            }
            string result = HttpContext.Current.Request[key];
            if (result == null)
            {

            }
            return Transform.ConvertValue<T>(result);

        }
        /// <summary>
        /// Encode a string to make is safe for rendering on a web page
        /// </summary>
        /// <param name="plainText">Unescaped text</param>
        /// <returns>Encoded text (or plainText if being called outside of an active HTTPContext)</returns>
        public static string HttpEncode(string plainText)
        {
            if (HttpContext.Current == null) return plainText;
            return HttpContext.Current.Server.HtmlEncode(plainText);
        }
        private const string dateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff";

        /// <summary>
        /// Convert a complete HttpRequest into an XML document string.  &lt;HttpRequest&gt; is first node returned.
        /// </summary>
        /// <param name="request">Request to parse.  If null then use HttpContext.Current</param>
        /// <returns>String of XML containing all fields within Request or empty &lt;HttpRequest /&gt; if Request can not be determined</returns>
        public static string HttpRequestToXml(HttpRequest request)
        {
            if (request == null)
            {
                if (HttpContext.Current == null) return "<HttpRequest />";
                if (HttpContext.Current.Request == null) return "<HttpRequest />";
                request = HttpContext.Current.Request; // get current request
            }

            StringBuilder sb = new StringBuilder();
            XmlWriter w = XmlWriter.Create(sb);

            //-------------------------------------------------------
            // <HttpRequest localTime="2007-07-15T13:15:01.321">
            //-------------------------------------------------------
            w.WriteStartElement("HttpRequest");

            w.WriteAttributeString("timeServerLocal", DateTime.Now.ToString(dateTimeFormat));
            w.WriteAttributeString("timeServerUtc", DateTime.Now.ToUniversalTime().ToString(dateTimeFormat));
            //-------------------------------------------------------
            // <serverVariables>
            //-------------------------------------------------------
            expandNVC(request.ServerVariables, "serverVariables", w);
            //-------------------------------------------------------
            // <acceptTypes>
            //      <acceptType>MIME</acceptType>
            //  </acceptTypes>
            //-------------------------------------------------------
            //w.WriteStartElement("acceptTypes");
            //foreach (string acceptType in request.AcceptTypes)
            //{
            //    w.WriteElementString("acceptType", acceptType);
            //}
            //w.WriteEndElement(); //<acceptTypes>

            w.WriteElementString("anonymousID", request.AnonymousID);

            //w.WriteElementString("applicationPath", request.ApplicationPath);
            //w.WriteElementString("appRelativeCurrentExecutionFilePath", request.AppRelativeCurrentExecutionFilePath);
            //w.WriteElementString("browser", request.Browser.Browser);
            //w.WriteElementString("clrVersion", request.Browser.ClrVersion.ToString(3));

            //-------------------------------------------------------
            // <certificate>
            //-------------------------------------------------------
            //expandNVC(request.ClientCertificate, "certificate", w);

            //-------------------------------------------------------
            // <encoding>
            //-------------------------------------------------------
            //w.WriteStartElement("encoding");
            //w.WriteElementString("bodyName", request.ContentEncoding.BodyName);
            //w.WriteElementString("codePage", request.ContentEncoding.CodePage.ToString());
            //w.WriteElementString("encodingName", request.ContentEncoding.EncodingName);
            //w.WriteElementString("contentLength", request.ContentLength.ToString());
            //w.WriteElementString("headerName", request.ContentEncoding.HeaderName);
            //w.WriteElementString("isBrowserDisplay", request.ContentEncoding.IsBrowserDisplay.ToString());
            //w.WriteElementString("isBrowserSave", request.ContentEncoding.IsBrowserSave.ToString());
            //w.WriteElementString("isMailNewsDisplay", request.ContentEncoding.IsMailNewsDisplay.ToString());
            //w.WriteElementString("isReadOnly", request.ContentEncoding.IsReadOnly.ToString());
            //w.WriteElementString("isSingleByte", request.ContentEncoding.IsSingleByte.ToString());
            //w.WriteElementString("webName", request.ContentEncoding.WebName);
            //w.WriteElementString("windowsCodePage", request.ContentEncoding.WindowsCodePage.ToString());
            //w.WriteEndElement(); //<encoding>

            w.WriteElementString("contentLength", request.ContentLength.ToString());
            w.WriteElementString("contentType", request.ContentType);

            //-------------------------------------------------------
            // <cookies>
            //-------------------------------------------------------
            //expandCookies(request.Cookies, w);

            w.WriteElementString("currentExecutionFilePath", request.CurrentExecutionFilePath);

            w.WriteElementString("filePath", request.FilePath);

            //-------------------------------------------------------
            // <files>
            //-------------------------------------------------------
            w.WriteStartElement("files");
            if (request.Files != null)
            {
                w.WriteAttributeString("count", request.Files.Count.ToString());
                for (int i = 0; i < request.Files.Count; i++)
                {
                    HttpPostedFile file = request.Files[i];
                    w.WriteStartElement("file");
                    w.WriteElementString("contentLength", file.ContentLength.ToString());
                    w.WriteElementString("contentType", file.ContentType);
                    w.WriteElementString("fileName", file.FileName);
                    w.WriteEndElement(); //</file>
                }
            }
            else
            {
                w.WriteAttributeString("count", "0");
            }
            w.WriteEndElement(); //</files>

            //-------------------------------------------------------
            // <form>
            //-------------------------------------------------------
            //expandNVC(request.Form, "form", w);

            //-------------------------------------------------------
            // <headers>
            //-------------------------------------------------------
            //expandNVC(request.Headers, "headers", w);

            //w.WriteElementString("httpMethod", request.HttpMethod);
            //w.WriteElementString("isAuthenticated", request.IsAuthenticated.ToString());
            //w.WriteElementString("isLocal", request.IsLocal.ToString());
            //w.WriteElementString("isSecureConnection", request.IsSecureConnection.ToString());

            //-------------------------------------------------------
            // <logonUserIdentity>
            //-------------------------------------------------------
            w.WriteStartElement("logonUserIdentity");
            if (request.LogonUserIdentity != null)
            {
                w.WriteElementString("authenticationType", request.LogonUserIdentity.AuthenticationType);
                w.WriteStartElement("groups");
                if (request.LogonUserIdentity.Groups != null)
                {
                    w.WriteAttributeString("count", request.LogonUserIdentity.Groups.Count.ToString());
                    for (int iGroup = 0; iGroup < request.LogonUserIdentity.Groups.Count; iGroup++)
                    {
                        IdentityReference iref = request.LogonUserIdentity.Groups[iGroup];
                        w.WriteElementString("identityReference", iref.Value);
                    }
                }
                else
                {
                    w.WriteAttributeString("count", "0");
                }
                w.WriteEndElement(); //</groups>

                w.WriteElementString("impersonationLevel", request.LogonUserIdentity.ImpersonationLevel.ToString());
                w.WriteElementString("isAnonymous", request.LogonUserIdentity.IsAnonymous.ToString());
                w.WriteElementString("isAuthenticated", request.LogonUserIdentity.IsAuthenticated.ToString());
                w.WriteElementString("isGuest", request.LogonUserIdentity.IsGuest.ToString());
                w.WriteElementString("isSystem", request.LogonUserIdentity.IsSystem.ToString());
                w.WriteElementString("name", request.LogonUserIdentity.Name);
                w.WriteStartElement("user");
                if (request.LogonUserIdentity.User != null)
                {
                    w.WriteElementString("accountDomainSid", request.LogonUserIdentity.User.AccountDomainSid.ToString());
                    w.WriteElementString("value", request.LogonUserIdentity.User.Value);
                }
                w.WriteEndElement();

            } //if LogonUserIdentity

            w.WriteEndElement(); //</logonUserIdentity>

            //-------------------------------------------------------
            // <params>
            //-------------------------------------------------------
            //expandNVC(request.Params, "params", w);

            //w.WriteElementString("path", request.Path);
            //w.WriteElementString("pathInfo", request.PathInfo);
            //w.WriteElementString("physicalApplicationPath", request.PhysicalApplicationPath);
            //w.WriteElementString("physicalPath", request.PhysicalPath);

            //-------------------------------------------------------
            // <queryString>
            //-------------------------------------------------------
            //expandNVC(request.QueryString, "queryString", w);

            //w.WriteElementString("rawUrl", request.RawUrl);
            //w.WriteElementString("requestType", request.RequestType);



            // w.WriteElementString("totalBytes", request.TotalBytes.ToString());

            //-------------------------------------------------------
            // <urlRequest>
            //-------------------------------------------------------
            //expandUri(request.Url, "urlRequest", w);

            //-------------------------------------------------------
            // <urlReferrer>
            //-------------------------------------------------------
            //expandUri(request.UrlReferrer, "urlReferrer", w);

            //w.WriteElementString("userAgent", request.UserAgent);
            //w.WriteElementString("userHostAddress", request.UserHostAddress);
            //w.WriteElementString("userHostName", request.UserHostName);

            //-------------------------------------------------------
            // <userLanguages>
            //-------------------------------------------------------
            //w.WriteStartElement("userLanguages");
            //foreach (string sLanguage in request.UserLanguages)
            //{
            //    w.WriteElementString("language", sLanguage);
            //}
            //w.WriteEndElement(); //</userLanguages>



            w.WriteEndElement(); //</httpRequest>

            w.Close();



            string sRequest = sb.ToString();
            return sRequest;

        }

        /// <summary>
        /// Convert HttpContext.Current.Request into an Xml delimited string
        /// </summary>
        /// <returns>Delmited string</returns>
        public static string HttpRequestToXml()
        {
            return HttpRequestToXml(null);
        }
        private static void expandUri(Uri uri, string parentTag, XmlWriter w)
        {
            w.WriteStartElement(parentTag);
            if (uri != null)
            {
                w.WriteElementString("absolutePath", uri.AbsolutePath);
                w.WriteElementString("absoluteUri", uri.AbsoluteUri);
                w.WriteElementString("authority", uri.Authority);
                w.WriteElementString("dnsSafeHost", uri.DnsSafeHost);
                w.WriteElementString("fragment", uri.Fragment);
                w.WriteElementString("host", uri.Host);
                w.WriteElementString("hostNameType", uri.HostNameType.ToString());

                w.WriteElementString("isAbsoluteUri", uri.IsAbsoluteUri.ToString());
                w.WriteElementString("isDefaultPort", uri.IsDefaultPort.ToString());
                w.WriteElementString("isFile", uri.IsFile.ToString());
                w.WriteElementString("isLoopback", uri.IsLoopback.ToString());
                w.WriteElementString("isUnc", uri.IsUnc.ToString());
                w.WriteElementString("localPath", uri.LocalPath);
                w.WriteElementString("originalString", uri.OriginalString);
                w.WriteElementString("pathAndQuery", uri.PathAndQuery);
                w.WriteElementString("port", uri.Port.ToString());
                w.WriteElementString("query", uri.Query);
                w.WriteElementString("scheme", uri.Scheme);
                w.WriteStartElement("segments");
                if (uri.Segments != null)
                {
                    w.WriteAttributeString("count", uri.Segments.Length.ToString());
                    foreach (string s in uri.Segments)
                    {
                        w.WriteElementString("segment", s);
                    }
                }
                else
                {
                    w.WriteAttributeString("count", "0");
                }
                w.WriteEndElement(); //</segments>
                w.WriteElementString("userEscaped", uri.UserEscaped.ToString());
                w.WriteElementString("userInfo", uri.UserInfo);
            }
            w.WriteEndElement(); //</parentTag>
        }

        private static void expandCookies(HttpCookieCollection cookies, XmlWriter w)
        {
            w.WriteStartElement("cookies");
            if (cookies != null)
            {
                w.WriteAttributeString("count", cookies.Count.ToString());
                for (int i = 0; i < cookies.Count; i++)
                {
                    HttpCookie c = cookies[i];
                    w.WriteStartElement("cookie");
                    w.WriteElementString("domain", c.Domain);
                    w.WriteElementString("expires", c.Expires.ToString(dateTimeFormat));
                    w.WriteElementString("hasKeys", c.HasKeys.ToString());
                    w.WriteElementString("httpOnly", c.HttpOnly.ToString());
                    w.WriteElementString("name", c.Name);
                    w.WriteElementString("path", c.Path);
                    w.WriteElementString("secure", c.Secure.ToString());
                    w.WriteElementString("value", c.Value);
                    expandNVC(c.Values, "values", w);
                    w.WriteEndElement(); //</cookie>
                }
            }
            else
            {
                w.WriteAttributeString("count", "0");
            }
            w.WriteEndElement(); //</cookies>
        }

        private static void expandNVC(NameValueCollection nvc, string parentTag, XmlWriter w)
        {
            w.WriteStartElement(parentTag);
            if (nvc != null)
            {
                w.WriteAttributeString("count", nvc.Count.ToString());
                for (int i = 0; i < nvc.Count; i++)
                {
                    var key = "";
                    var value = "";
                    try
                    {
                        key = nvc.AllKeys[i];
                        if (string.IsNullOrEmpty(key))
                            key = "nullKey";
                        value = nvc.Get(i);
                        w.WriteElementString(XmlConvert.EncodeLocalName(key), value);
                    }
                    catch (ArgumentException ex)
                    {

                        w.WriteElementString("invalidKey", string.Format("key:{0} value: '{1}'", key, value));
                    }
                }
            }
            else
            {
                w.WriteAttributeString("count", "0");
            }
            w.WriteEndElement(); //</parentTag>

        }
        /// <summary>
        /// Expand an HTTP request into a well formatted string
        /// </summary>
        /// <param name="request">Request to expand.  If <paramref name="request"/> is null, then use HttpContext.Current (if available)</param>
        /// <returns>Expanded request or 'Not Available' if not found</returns>
        public static string Expand(HttpRequest request)
        {
            StringBuilder sb = new StringBuilder();
            if (request == null)
            {
                if (HttpContext.Current != null)
                {
                    request = HttpContext.Current.Request;
                }
            }
            if (request == null)
            {
                sb.AppendLine("HttpRequest: (not available)");
                return sb.ToString();
            }

            expand(request.ServerVariables, "Server", sb); //EXTENSION: add more details if desired
            return sb.ToString();
        }
        private static void expand(NameValueCollection nvc, string parentTag, StringBuilder sb)
        {
            for (int i = 0; i < nvc.Count; i++)
            {
                var key = "";
                var value = "";
                
                key = nvc.AllKeys[i];
                if (key == "ALL_RAW" || key == "ALL_HTTP") continue;
                if (string.IsNullOrEmpty(key))
                    key = "nullKey";
                value = nvc.Get(i);
                sb.AppendLine("HttpRequest:{0}[{1}]={2}", parentTag, key, value);

            }
        }

        public static NameValueCollection ToList(HttpRequest request)
        {
            NameValueCollection list = new NameValueCollection();
            if (request == null)
            {
                if (HttpContext.Current != null)
                {
                    request = HttpContext.Current.Request;
                }
            }
            if (request == null)
            {
                list.Add("", "(http request not found)");
                return list;
            }
            for (int x = 0; x < request.ServerVariables.Count; x++)
            {
                string key = request.ServerVariables.AllKeys[x];
                string value = request.ServerVariables.Get(x);
                list.Add(string.Format("Server[{0}]", key), value);
            }
            return list;
        }
    } //class WebUtils
}
