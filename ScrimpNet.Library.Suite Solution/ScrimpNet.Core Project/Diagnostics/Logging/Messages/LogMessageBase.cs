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
using System.Linq;
using System.Text;
using System.Diagnostics;
using ScrimpNet.Reflection;
using ScrimpNet.Text;
using System.Runtime.Serialization;

namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// Generic container for a log message.  All standard messages implmement these fields
    /// </summary>
    [Serializable]
    [DataContract(Namespace = CoreConfig.WcfNamespace)]
    public partial class LogMessage
    {
        private AssemblyVersion.CallerInfo _callerInfo;
        /// <summary>
        /// Default constructor
        /// </summary>
        public LogMessage()
        {
            // set default properties and force loading some properties from .config file
            this.MessageType = this.GetType().FullName;
            this.HostName = System.Environment.MachineName;
            this.TimeStamp = DateTime.Now;
            this.ActivityId = this.ActivityId; // force loading of ActivityId from base.Correlation manager
            this.ApplicationKey = this.ApplicationKey;
            this.Priority = this.Priority;
            this.ActiveEnvironment = this.ActiveEnvironment;
            _callerInfo = new AssemblyVersion().CallerInfoGet();
            this.Categories = new List<string>();
            this.ExtendedProperties = new Dictionary<string, string>();
            this.MessageId = Guid.NewGuid();

        }

        /// <summary>
        /// Unique identifier for this message.  Used when comparing messages in different message sinks.
        /// Used in IT departments where there is a preference to log by number instead of by category and/or name
        /// </summary>
        [DataMember]
        public Guid MessageId { get; set; }

        private Guid _relatedActivityId = CoreConfig.ActivityId;
        /// <summary>
        /// Id that enables end-to-end identification of a common thread of messages.  Uses the Trace.CorrelationManager
        /// </summary>        
        [DataMember]
        public Guid ActivityId
        {
            get
            {
                return _relatedActivityId;
            }
            set
            {
                Trace.CorrelationManager.ActivityId = value;
                _relatedActivityId = value;
            }
        }


        private string _loggerName;
        /// <summary>
        /// Name of logger creating this message.  Often used for identifying which part of application this message came from since logger names often are identified with application functions
        /// </summary>
        [DataMember]
        public string LoggerName
        {
            get
            {
                return _loggerName;
            }
            set
            {
                _loggerName = value;
            }
        }

        private string _environmentName = CoreConfig.ActiveEnvironment;
        /// <summary>
        /// Returns the currently active environment the application is running in (dev, qa, production, demo, etc) (Default: .config ScrimpNet.Application.Environment)
        /// </summary>
        [DataMember]
        public string ActiveEnvironment
        {
            get { return _environmentName; }
            set { _environmentName = value; }
        }

        private string _hostName = System.Environment.MachineName;
        /// <summary>
        /// Gets or sets the name of the host (Machine Name).
        /// </summary>
        [DataMember]
        public string HostName
        {
            get { return _hostName; }
            set { _hostName = value; }
        }

        /// <summary>
        /// Subsection of application (if any) (e.g. Credit Cards, Travel, etc)
        /// </summary>
        [DataMember]
        public string ApplicationSubKey { get; set; }

        private string _applicationKey = CoreConfig.ApplicationKey;

        /// <summary>
        /// Gets the name of the application.  If not explicitly set using &lt;appSettings name="ScrimpNet.Application.Name"...&gt;
        /// </summary>
        [DataMember]
        public string ApplicationKey
        {
            get
            {
                if (string.IsNullOrEmpty(_applicationKey) == true)
                {
                    _applicationKey = CoreConfig.ApplicationKey;
                }
                return _applicationKey;
            }
            set
            {
                _applicationKey = value;
            }
        }

        private MessageLevel _logLevel = MessageLevel.Information;
        /// <summary>
        /// Severity of message being persisted
        /// </summary>
        [DataMember]
        public MessageLevel Severity
        {
            get { return _logLevel; }
            set
            {
                _logLevel = value;
                priorityReset();
            }
        }

        private MessagePriority? _priority = null;
        /// <summary>
        /// Get and Sets how important the sender of this message considers this message.  Most implementations
        /// will not explictly set this value and allow message to determine priority based on log levels.
        /// </summary>
        [DataMember]
        public MessagePriority Priority
        {
            get
            {
                if (_priority.HasValue == true) return _priority.Value;
                return priorityReset();

            }
            set
            {
                _priority = value;
            }
        }
        private DateTime _createDate = DateTime.Now;
        /// <summary>
        /// Date/Time when this message was created
        /// </summary>
        [DataMember]
        public DateTime TimeStamp
        {
            get { return _createDate; }
            set { _createDate = Utils.Date.ToSqlDate(value); }
        }
        /// <summary>
        /// Resets internal priority to a value that matches LogLevel of this message
        /// </summary>
        /// <returns>Messages current priority</returns>
        internal MessagePriority priorityReset()
        {
            switch (this.Severity)
            {
                case MessageLevel.Critical: _priority = MessagePriority.Highest; break;
                case MessageLevel.Error: _priority = MessagePriority.VeryHigh; break;
                case MessageLevel.Warning: _priority = MessagePriority.High; break;
                case MessageLevel.Information: _priority = MessagePriority.Normal; break;
                case MessageLevel.Trace: _priority = MessagePriority.Low; break;
                case MessageLevel.Debug: _priority = MessagePriority.VeryLow; break;

                default:
                    _priority = MessagePriority.Normal; break;
            }
            return _priority.Value;
        }

        private string _messageType = string.Empty;
        /// <summary>
        /// .Net type name of this message.  Used for serialization purposes and set in constructors
        /// </summary>
        [DataMember]
        public string MessageType
        {
            get
            {
                if (string.IsNullOrEmpty(_messageType) == true)
                {
                    _messageType = this.GetType().FullName;
                }
                return _messageType;
            }
            set
            {
                _messageType = value;
            }
        }

        private string _messageText = "";

        /// <summary>
        /// Miscellaneous key-value pairs that the application wants to include as part of the message
        /// </summary>
        [DataMember]
        public Dictionary<string, string> ExtendedProperties { get; set; }

        /// <summary>
        /// Returns explictly set message text or default of this class (usually ToString())
        /// </summary>
        [DataMember]
        public string MessageText
        {
            get
            {
                if (_messageText == null)
                {
                    return ToString();
                }
                return _messageText;
            }
            set
            {
                _messageText = value;
            }
        }
        /// <summary>
        /// Detailed information about the callstack of method calling this class
        /// </summary>
        [DataMember]
        public AssemblyVersion.CallerInfo CallerInfo
        {
            get
            {
                return _callerInfo;
            }
            set
            {
                _callerInfo = value;
            }
        }

        [DataMember]
        public List<string> Categories { get; set; }

        /// <summary>
        /// Standard format for this class
        /// </summary>
        /// <returns>String value of this class</returns>
        public new virtual string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{0:yyyy-MM-dd HH:mm:ss.fff} {4}:: {1}({2}) {3}", this.TimeStamp, this.Severity, this.Priority, _messageText.Left(60, "..."), Utils.Date.TZAbbreviation);
            sb.AppendLine("   Full Message: {0}", indentRow(this._messageText, 17));
            sb.AppendLine("       Severity: {0}", this.Severity);
            sb.AppendLine("       Priority: {0}", this.Priority);
            sb.AppendLine("    Application: {0}", this.ApplicationKey);
            sb.AppendLine("    Activity Id: {0}", this.ActivityId);
            sb.AppendLine("    Environment: {0}", this.ActiveEnvironment);
            sb.AppendLine("        Machine: {0}", this.HostName);
            sb.AppendLine("    Logger Name: {0}", this.LoggerName);
            sb.AppendLine("   Message Type: {0}", this.MessageType);
            sb.AppendLine("     Categories: Count({0})", (Categories == null) ? 0 : Categories.Count);
            for (int x = 0; x < Categories.Count; x++)
            {
                sb.AppendLine("            {0}: {1}", x, Categories[x]);
            }
            if (ExtendedProperties != null)
            {
                foreach (string key in ExtendedProperties.Keys)
                {
                    sb.AppendLine("   {0}:{1}", key, ExtendedProperties[key].ToString().Replace(Environment.NewLine, Environment.NewLine + "      "));
                }
            }
            sb.Append(_callerInfo.ToString());
            return sb.ToString();
        }

        protected string indentRow(string sourceString, int spaceCount)
        {
            if (string.IsNullOrEmpty(sourceString) == true) return sourceString;
            return sourceString.Replace(Environment.NewLine, Environment.NewLine + (new string(' ', spaceCount)));
        }
    }
}
