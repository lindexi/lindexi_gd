/*
// ScrimpNet.Core Library
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
using System.Linq;
using System.Text;
using ScrimpNet.Text;
using System.Runtime.Serialization;

namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// Defines a single message to be persisted into log.  This class is used in application logging scenarios
    /// </summary>
    [DataContract(Namespace = CoreConfig.WcfNamespace)]
    public partial class ApplicationLogMessage : LogMessage
    {
        #region [Constructor(s)]

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationLogMessage()
            : base()
        {
           
        }

        /// <summary>
        /// Create a LogMessage with user defined text
        /// </summary>
        /// <param name="messageNumber">Numerical identifier of this message (e.g. '101', '34334')</param>
        /// <param name="messageText">Textual portion of message being persisted.  May contain string.format({0}) place holders</param>
        /// <param name="args">Arguments to be supplied to <paramref name="messageText"/>></param>
        public ApplicationLogMessage(string messageNumber, string messageText, params object[] args)
            : this()
        {
            MessageText = TextUtils.StringFormat(messageText, args);
            MessageNumber = messageNumber;
        }

        /// <summary>
        /// Create a LogMessage with user defined text
        /// </summary>
        /// <param name="messageText">Textual portion of message being persisted.  May contain string.format({0}) place holders</param>
        /// <param name="args">Arguments to be supplied to <paramref name="messageText"/></param>
        public ApplicationLogMessage(string messageText, params object[] args)
            : this("", messageText, args)
        {
        }

        #endregion
        private string _messageNumber =string.Empty;
        /// <summary>
        /// Identifier of this message (e.g. '101', '34334')
        /// </summary>
        [DataMember]
        public string MessageNumber { get { return _messageNumber; } set { _messageNumber = value; } }

        private string _title = string.Empty;
        /// <summary>
        /// Short text of message. Also considered "subject" or "label" depending on sending via email or via msmq respectively.  If not explicitly set, 
        /// title will attempt to build return value from set values
        /// </summary>
        [DataMember]
        public virtual string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_title) == false) return _title;
                _title = TextUtils.StringFormat("{0} {1}({2}) {3:0000} ",
                        this.ApplicationKey,
                        this.Severity.ToString(),
                        this.Priority.ToString(),
                        this.MessageNumber
                        );
                _title = _title.Left(70, "...");
                return _title;
            }
            set
            {
                _title = value;
            }
        }

        private Exception _exception = null;
        /// <summary>
        /// Exception (if any) this message is referring to.
        /// </summary>
        [System.Xml.Serialization.XmlElement]
        public Exception Exception { get { return _exception; } set { _exception = value; } }

        RuntimeContext _runtimeContext = null;
        /// <summary>
        /// Place holder for gathering run time information.  Note:  This method is very heavy and should be
        /// used cautiously.  Use <see cref="T:RuntimeContext"/> constructors for information
        /// on capturing contexts.  If this field is populated, logging providers will generally persist
        /// values.
        /// </summary>
        [System.Xml.Serialization.XmlElement]
        [DataMember]
        public RuntimeContext RuntimeContext
        {
            get { return _runtimeContext; }
            set { _runtimeContext = value; }
        }

        /// <summary>
        /// Generates a standard output string.  EXTENSION:  Modify this class to change the format of the message being persisted
        /// </summary>
        /// <returns>A standard format of this message type</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.Append(base.ToString());
                if (this.Exception != null)
                {
                    sb.AppendFormat("   Exception: {0}{1}", Utils.Expand(this.Exception, 3), Environment.NewLine);
                }
                else
                {
                    sb.AppendFormat("   Exception: {0}{1}", "(none)", Environment.NewLine);
                }
                if (this.RuntimeContext != null)
                {
                    if (this.Exception == null)
                    {                 
                        sb.AppendLine("   Machine:Stack: {0}", indentRow(this.RuntimeContext.MachineContext["StackTrace"],8));
                    }
                    else
                    {
                        sb.AppendLine("   Machine:Stack: {0}", "(see exception)");
                    }

                    for (int x = 0; x < RuntimeContext.MachineContext.Count; x++)
                    {
                        string key = RuntimeContext.MachineContext.Keys[x];
                        if (key == "StackTrace") continue;  //this will be logged further down
                        string value = RuntimeContext.MachineContext.Get(x);
                        sb.AppendLine("   Machine[{0}]={1}", key, indentRow(value,8));
                    }

                }
               
                if (this.RuntimeContext != null && RuntimeContext.HttpRequest.Count > 0)
                {
                    for (int x = 0; x < RuntimeContext.HttpRequest.Count; x++)
                    {
                        string key = RuntimeContext.HttpRequest.Keys[x];
                        string value = RuntimeContext.HttpRequest.Get(x);
                        sb.AppendLine("   HttpRequest[{0}]={1}", key, indentRow(value,8));
                    }                   
                }              
            }
            catch (Exception ex)
            {
                Log.LastChanceLog(Utils.Expand(ex));
                Log.LastChanceLog(sb.ToString());
            }
            return sb.ToString();
        }
   
    }
}
