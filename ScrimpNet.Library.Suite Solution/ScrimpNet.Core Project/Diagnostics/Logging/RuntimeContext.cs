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
using System.Xml.Serialization;
using System.Xml;
using ScrimpNet.Diagnostics;
using ScrimpNet;
using ScrimpNet.Diagnostics.Logging;
using ScrimpNet.Web;
using ScrimpNet.IO;
using ScrimpNet.Text;
using System.Runtime.Serialization;
using System.Collections.Specialized;

namespace ScrimpNet.Diagnostics
{
        /// <summary>
        /// Determines what kind of contexual information to probe for
        /// </summary>
            [DataContract(Namespace = CoreConfig.WcfNamespace)]
        public enum RuntimeContextOptions
        {
            /// <summary>
            /// Do not perform additional information on creation or on serialization
            /// </summary>
            [EnumMember]
            None = 0,

            /// <summary>
            /// Gather detailed HTTP Request at the moment of instantiation.  XmlSerializer will serialize this context
            /// </summary>
            [EnumMember]
            HttpRequestContext = 1,

            /// <summary>
            /// Gather detail runtime and stack information at the moment of instantiation  XmlSerializer will serialize this context
            /// </summary>
            [EnumMember]
            MachineContext = 2,
        }

        /// <summary>
        /// Represents operating system environment at time of log entry
        /// </summary>
            [DataContract(Namespace = CoreConfig.WcfNamespace)]
        public class RuntimeContext : IXmlSerializable, IFormattable
        {

            /// <summary>
            /// Facade accessor for CoreConfig.ActiveEnvironment.  Returns current value
            /// of .config Environment variable
            /// </summary>
            public static string ActiveEnvironment
            {
                get
                {
                    return CoreConfig.ActiveEnvironment;
                }               
            }

            /// <summary>
            /// Gets default formatter for RuntimeContext
            /// </summary>
            public static MachineContextFormatter Formatter
            {
                get
                {
                    return new MachineContextFormatter();
                }
            }
            /// <summary>
            /// Default constructor.  Probes system for required context information defined in options
            /// </summary>
            public RuntimeContext(RuntimeContextOptions options)
            {
                if ((options & RuntimeContextOptions.MachineContext) == RuntimeContextOptions.MachineContext)
                {
                    _machineContext = (new MachineContext()).ToList();

                }
                if ((options & RuntimeContextOptions.HttpRequestContext) == RuntimeContextOptions.HttpRequestContext)
                {
                    _httpRequest = WebUtils.ToList(null);
                }
            }

            NameValueCollection _httpRequest = new NameValueCollection();

            //private string _httpRequest;
            /// <summary>
            /// XML string containing details of the current HTTP request, if one can be found or null if not found or not specified in options constructor
            /// </summary>
            [DataMember]
            public NameValueCollection HttpRequest
            {
                get { return _httpRequest; }
                set { _httpRequest = value; }
            }


            private NameValueCollection _machineContext;
            /// <summary>
            /// Settings of the current execute context or null if not specified in options constructor
            /// </summary>
            [DataMember]
            public NameValueCollection MachineContext
            {
                get { return _machineContext; }
                set { _machineContext = value; }
            }

            #region IXmlSerializable Members

            /// <summary>
            /// IXmlSerializable Interface Implementation
            /// </summary>
            /// <returns>Null</returns>
            public System.Xml.Schema.XmlSchema GetSchema()
            {
                return null;
            }

            /// <summary>
            /// Not Implmented.
            /// </summary>
            /// <param name="reader">Deserializion Xml Reader</param>
            public void ReadXml(XmlReader reader)
            {
                throw new NotImplementedException("This class is intended to be serialized only.  Deserialization is not supported");
            }

            /// <summary>
            /// Generate an XML representation of this class. &gt;RuntimeContext&lt; is the first node returned.
            /// </summary>
            /// <param name="writer">Writer to which Xml of this object is sent</param>
            public void WriteXml(XmlWriter writer)
            {
                try
                {
                    writer.WriteStartElement("RuntimeContext");
                    //if (string.IsNullOrEmpty(_httpRequest) == true)
                    //{
                    //    writer.WriteRaw(@"<HttpRequest />");
                    //}
                    //else
                    //{
                    //    writer.WriteRaw(_httpRequest);
                    //}
                    //if (_machineContext != null)
                    //{
                    //    writer.WriteRaw(_machineContext.ToXml());
                    //}
                    //else
                    //{
                    //    writer.WriteRaw(@"<RuntimeContext/>");
                    //}
                }
                finally
                {
                    writer.WriteEndElement(); //</RuntimeContext>
                }
            }

            /// <summary>
            /// Converts this object into an Xml string
            /// </summary>
            /// <returns>String of this object coverted to Xml</returns>
            public string ToXml()
            {
                return Serialize.To.Xml(this);
            }

            /// <summary>
            /// Return a string representing this object. (Same as ToXml())
            /// </summary>
            /// <returns>String representing this object</returns>
            public override string ToString()
            {
                return this.GetType().FullName;
            }
            //public string ToString(string format, IFormatProvider provider)
            //{

            //}
            #endregion

            #region IFormattable Members

            //string IFormattable.ToString(string format, IFormatProvider formatProvider)
            //{
            //    throw new Exception("The method or operation is not implemented.");
            //}

            #endregion

            #region IFormattable Members

            /// <summary>
            /// Convert a runtime context to a specified format string
            /// </summary>
            /// <param name="format">Format string that determines who a context will be returned</param>
            /// <param name="formatProvider">Unique provider for this context</param>
            /// <returns>Formatted string</returns>
            /// <exception cref="InvalidOperationException">Thrown if formatProvider is not MachineContextFormatter</exception>
            public string ToString(string format, IFormatProvider formatProvider)
            {
                MachineContextFormatter fmt = formatProvider as MachineContextFormatter;
                if (fmt == null)
                {
                    throw new InvalidOperationException(TextUtils.StringFormat("Unable to convert {0} to RunTimeContextFormatter", formatProvider.GetType().FullName));
                }
                return fmt.Format(format, this, formatProvider);
            }

            /// <summary>
            /// Return a string representing this runtime context
            /// </summary>
            /// <param name="format">Describes which fields will be returned</param>
            /// <returns>Formatted runtime context</returns>
            public string ToString(string format)
            {
                return ToString(format, new MachineContextFormatter());
            }

            #endregion
        }
}
