using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrimpNet;
using System.Collections;
using ScrimpNet.Text;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace ScrimpNet
{

    /// <summary>
    /// A DTO that returns state (Error, Success) and data property bag back to caller.  Modeled after ASP.Net MVC
    /// ViewDataCollection.  Use this class as foundation for returning data/state back to caller (specifically
    /// code behind pages)
    /// </summary>
    [DataContract]
    public partial class ActionReply : ICollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable, IDataErrorInfo
    {
        Dictionary<string, object> _dataItems = new Dictionary<string, object>();

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActionReply()
            : base()
        {
            Messages = new ActionReplyMessageList();
        }
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="result">Source to copy</param>
        public ActionReply(ActionReply result)
            : this()
        {
            Value = result.Value;
        }
        /// <summary>
        /// Add a exception to model messages
        /// </summary>
        /// <param name="ex">Exception to add</param>
        public ActionReply(Exception ex)
        {
            Messages.Add(ex);
            this.Status = ActionStatus.InternalError;
        }
        /// <summary>
        /// Set the Model parameter with obj
        /// </summary>
        /// <param name="obj">Value to initialize Model parameter with</param>
        public ActionReply(object obj)
            : this()
        {
            Value = obj;
        }

        private ActionStatus _status = ActionStatus.Unknown;
        /// <summary>
        /// General status of Request from the perspective of the
        /// services. Value is most severe between explicitly set and values associated with messages.  
        /// NOTE: Property can not be lowered in value once set to a higher value.
        /// </summary>
        [DataMember]
        public ActionStatus Status
        {
            get
            {
                return (ActionStatus)Math.Max((int)_status, (int)Messages.MaximumSeverity);
            }
            set
            {
                _status = (ActionStatus)(Math.Max((int)_status, (int)value));
            }
        }

        private Guid _correlationId = CoreConfig.ActivityId;
        /// <summary>
        /// System correlation context this message is created under
        /// </summary>
        [DataMember]
        public Guid CorrelationId
        {
            get
            {
                return _correlationId;
            }
            set
            {
                _correlationId = value;
            }
        }

        /// <summary>
        /// Data store that returns any data back to the caller
        /// </summary>
        [DataMember]
        public object Value { get; set; }

        /// <summary>
        /// Messages or status (error, success), if any, called method wants to return.  ActionReply
        /// is considered to be in 'Valid/Success' state if there are no entries in Messages list or all
        /// entries are informational in nature
        /// </summary>
        [DataMember]
        public ActionReplyMessageList Messages { get; set; }

        /// <summary>
        /// True if there are no entries in Messages or all
        /// entries are informational in nature and ActionStatus has not been set to an error condition
        /// </summary>
        public bool IsValid
        {
            get
            {
                return Messages.IsValid && (int)Status < 400;
            }
        }

        /// <summary>
        /// Indexer to retrieve value from response values.  Note: this method returns any DATA items registered. this[key] returns any MESSAGES with key.
        /// </summary>
        /// <param name="key">Index of item to get</param>
        /// <returns>Object at key</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when key does not exist response</exception>
        public object DataItem(string key)
        {
            object o;
            if (_dataItems.TryGetValue(key, out o) == false)
            {
                throw new IndexOutOfRangeException(TextUtils.StringFormat("Unable to find key '{0}' in property list", key));
            }
            return o;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dataItems.GetEnumerator();
        }

        /// <summary>
        /// IDataErrorInfo: Returns piple '|' delimited list of error messages or NULL if no error message
        /// </summary>
        public string Error
        {
            get
            {
                return Messages.Error;
            }
        }

        /// <summary>
        /// Returns any message associated with <paramref name="columnName"/>.  
        /// Note: this method returns any MESSAGES for key.  DataItem(key) returns DATA associated with this response
        /// </summary>
        /// <param name="columnName">Name of field or reference id for message to return</param>
        /// <returns>Message for <paramref name="columnName"/> or null if not found</returns>
        public string this[string columnName]
        {
            get
            {
                return Messages[columnName];
            }
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _dataItems.GetEnumerator();
        }
        /// <summary>
        /// Add a data item to internal property bag
        /// </summary>
        /// <param name="item">Hydrated key/value pair</param>
        public void Add(KeyValuePair<string, object> item)
        {
            _dataItems.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Any data to return to caller if not included in Value.  Patterned from MVC.Net response object
        /// </summary>
        public Dictionary<string, object> Data
        {
            get
            {
                return _dataItems;
            }
            set
            {
                _dataItems = value;
            }
        }

        /// <summary>
        /// Empty all data items (internal property bag)
        /// </summary>
        public void Clear()
        {
            _dataItems.Clear();
        }

        /// <summary>
        /// Checks to see if an item is a member of internal property bag
        /// </summary>
        /// <param name="item">Value to search for</param>
        /// <returns>True if found</returns>
        public bool Contains(KeyValuePair<string, object> item)
        {
            return _dataItems.Contains(item);
        }

        /// <summary>
        /// Add/Replace a key/value pair in the data collection
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, object value)
        {
            _dataItems[key] = value;
        }

        /// <summary>
        /// NOT implemented.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns number of items in internal property bag
        /// </summary>
        public int Count
        {
            get { return _dataItems.Count; }
        }

        /// <summary>
        /// Always returns FALSE
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes an item from internal property bag
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<string, object> item)
        {
            return _dataItems.Remove(item.Key);
        }


        //-----------------------------------------------------------
        // convenience methods
        //-----------------------------------------------------------

        /// <summary>
        /// Merge source values (messages, exceptions, etc) into this reply setting status to most severe
        /// </summary>
        /// <param name="source">Values to be merged into this reply</param>
        /// <returns>Reference to this object with content merged</returns>
        public virtual ActionReply Merge(ActionReply source)
        {

            foreach (var msg in source.Messages)
            {
                this.Messages.Add(msg);
            }

            this.Status = source.Status;
            this.CorrelationId = source.CorrelationId;
            this.Value = source.Value ?? this.Value;

            foreach (var kvp in source)
            {
                this.Add(kvp);
            }
            return this;
        }
        /// <summary>
        /// Captures exception and sets ActionStatus.InternalError
        /// </summary>
        /// <param name="ex">Exception to add to messages list</param>
        public virtual ActionReply SetError(Exception ex)
        {
            return SetError(ex, ActionStatus.InternalError);
        }

        /// <summary>
        /// Captures exception and sets action severity
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="severity"></param>
        public virtual ActionReply SetError(Exception ex, ActionStatus severity)
        {
            Messages.Add(ex, severity);
            Status = severity;
            return this;
        }

        /// <summary>
        /// Set the status with a (optional) message and return the object (convenience method)
        /// </summary>
        /// <param name="status">Status to set response to</param>
        /// <param name="message">Text to be added to this response</param>
        /// <param name="args">List of arugments to supply to message</param>
        /// <returns>reference to this object</returns>
        public virtual ActionReply SetStatus(ActionStatus status, string message, params object[] args)
        {

            this.Messages.Add(status, message, args);
            return this;
        }

        /// <summary>
        /// Captures exception, sets severity, and writes reply to log.  (Convenience method)
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        /// <returns></returns>
        public virtual ActionReply SetErrorAndLog(Exception exception, ActionStatus severity)
        {
            Messages.Add(exception, severity);
            Status = severity;
            LogWrite();
            return this;
        }
        /// <summary>
        /// Captures exception, sets severity (ActionStatus.InternalError), and writes reply to log.  (Convenience method)
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public virtual ActionReply SetErrorAndLog(Exception exception)
        {
            Messages.Add(exception, ActionStatus.InternalError);
            Status = ActionStatus.InternalError;
            LogWrite();
            return this;
        }

        /// <summary>
        /// Convenience method to add formatted parameters values to reply.
        /// Default severity: information
        /// </summary>
        /// <param name="paramName">Name of parameter</param>
        /// <param name="paramValue">Value of parameter</param>
        public void AddParam(string paramName, object paramValue)
        {
            AddParam(ActionStatus.Information, paramName, paramValue);
        }

        /// <summary>
        /// Convenience method to add formatted parameter values to reply.  Default severity: information
        /// </summary>
        /// <param name="severity">Describes how important this parameter note is to be considered</param>
        /// <param name="paramName">Name of property or parameter</param>
        /// <param name="paramValue">Current value of parameter</param>
        public void AddParam(ActionStatus severity, string paramName, object paramValue)
        {
            ActionReplyMessageItem item = new ActionReplyMessageItem(paramName, severity, "Param: {0}: {1}", paramName, paramValue);
            item.Severity = severity;
            Messages.Add(item);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (this.Value != null)
            {
                sb.AppendLine("ActionReply<{0}>", this.Value.GetType().ToString());
            }
            else
            {
                sb.AppendLine("ActionReply");
            }
            if (this.IsValid == false)
            {
                sb.AppendLine("Error(s): {0}", this.Error);
            }
            sb.AppendLine("Status: {0}", this.Status.ToString());
            sb.AppendLine("IsValid: {0}", this.IsValid.ToString());
            sb.AppendLine("CorrelationId: {0}", this.CorrelationId.ToString());
            sb.AppendLine("Messages: Total: {0}", this.Messages.Count());
            for (int x = 0; x < Messages.Count; x++)
            {
                var msg = Messages[x];
                sb.AppendLine("  Message[{0}] {2} {1}", x, msg.MessageText, msg.Severity.ToString());
                if (msg.Exception != null)
                {
                    sb.Append("     {0}", msg.Exception.Expand(5));
                }
            }
            if (Value != null)
            {
                try
                {
                    string s = Serialize.To.Xml(Value);
                    sb.AppendLine("Value ({0}): {1}", Value.GetType().Name, s);
                }
                catch (Exception ex)
                {
                    try
                    {
                        string s = Serialize.To.DataContract(Value);
                        sb.AppendLine("Error trying to serialize {0}. {1}", Value.GetType().Name, ex.ToString());
                    }
                    catch
                    {
                        try
                        {
                            string s = Serialize.To.Soap(Value);
                            sb.AppendLine("Error trying to serialize {0}. {1}", Value.GetType().Name, ex.ToString());
                        }
                        catch (Exception ex2)
                        {
                            string s = Value.ToString();
                            sb.AppendLine("Value<{0}>.ToString(): ", Value.GetType().Name, Value.ToString());
                            sb.AppendLine("Error trying to serialize {0}. {1}", Value.GetType().Name, ex2.ToString());
                        }
                    }
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A DTO that returns state (Error, Success) and data property bag back to caller.  Modeled after ASP.Net MVC
    /// ViewDataCollection.  Use this class as foundation for returning data/state back to caller (specifically
    /// code behind pages).  Use this class if returning a strongly typed Model contained in 'Value' property
    /// </summary>
    /// <typeparam name="T">Type the 'Value' property will take.</typeparam>
    [DataContract]
    public partial class ActionReply<T> : ActionReply where T : class
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ActionReply()
            : base()
        {

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="result"></param>
        public ActionReply(ActionReply<T> result)
            : base(result)
        {

        }

        /// <summary>
        /// Create new reply with ActionStatus.InternalError
        /// </summary>
        /// <param name="ex"></param>
        public ActionReply(Exception ex)
            : base(ex)
        {
        }

        /// <summary>
        /// Captures exception and sets ActionStatus.InternalError
        /// </summary>
        /// <param name="exception">Exception to add to messages list</param>
        public new ActionReply<T> SetError(Exception exception)
        {
            return this.SetError(exception, ActionStatus.InternalError);
        }
        public new ActionReply<T> SetErrorAndLog(Exception exception)
        {
            return this.SetErrorAndLog(exception, ActionStatus.InternalError);
        }
        /// <summary>
        /// Captures exception details.  Use when wanting to set severity level to something other than default: ActionStatus.InternalError
        /// </summary>
        /// <param name="exception">Exception to add to messages list</param>
        /// <param name="severity">Message severity level.</param>
        public new ActionReply<T> SetError(Exception exception, ActionStatus severity)
        {
            return (ActionReply<T>)base.SetError(exception, severity);
        }

        /// <summary>
        /// Merge source values (messages, exceptions, etc) into this reply setting status to most severe
        /// </summary>
        /// <param name="source">Values to be merged into this reply</param>
        /// <returns>Reference to this object with content merged</returns>
        public ActionReply<T> Merge(ActionReply<T> source)
        {
            return (ActionReply<T>)base.Merge(source);
        }
        public new ActionReply<T> Merge(ActionReply source)
        {
            return (ActionReply<T>)base.Merge(source);
        }
        /// <summary>
        /// Set severity to ActionStatus.Error and automatically log reply to configured ScrimpNet.Log (Convenience method)
        /// </summary>
        /// <param name="exception">Exception that will be associated with this reply.  Will append to any previously caught exceptions</param>
        /// <param name="severity">How important caller considers this exception</param>
        /// <returns>Reference to this object with exception and severity level set</returns>
        public new ActionReply<T> SetErrorAndLog(Exception exception, ActionStatus severity)
        {
            return (ActionReply<T>)base.SetErrorAndLog(exception, severity);
        }

        /// <summary>
        /// Gets/sets strongly typed object to use as the 'Model' property
        /// </summary>
        [DataMember]
        public new T Value
        {
            get
            {
                return (T)base.Value;
            }
            set
            {
                base.Value = value;
            }
        }
    }
}
