using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace ScrimpNet
{
    /// <summary>
    /// List of states (error, info, etc) with associated messages.  Generally a service call will
    /// create one or more states for return back to caller that indicates result of call.  This
    /// is similar to ASP.Net MVC ModelStates
    /// </summary>
    [CollectionDataContract]
    public class ActionReplyMessageList : List<ActionReplyMessageItem>, IDataErrorInfo
    {

        private ActionStatus _status = ActionStatus.Unknown;
        /// <summary>
        /// Returns the maximum severtiy of all messages.  NOTE:  Updated when a message is added.  If Message is removed or MessageItem modified this value doesn't refresh.  Refresh is TBD when use-case is defined.
        /// </summary>
        [DataMember]
        public ActionStatus MaximumSeverity
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
            }
        }

        private void setStatus(ActionStatus newStatus)
        {
            if (_status < newStatus)
            {
                _status = newStatus;
            }
        }

        /// <summary>
        /// Force a refresh of maximum status.  Use when removing messages or modifying existing message severity
        /// </summary>
        public void RefreshMaximumStatus()
        {
            _status = ActionStatus.Unknown;
            foreach (ActionReplyMessageItem item in this)
            {
                setStatus(item.Severity);
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActionReplyMessageList()
        {
        }

        /// <summary>
        /// Adds a message to the list.
        /// </summary>
        /// <param name="severity">Severity of the message</param>
        /// <param name="message">Text to be associated with this state</param>
        /// <param name="args">Optional arguments to be supplied to message</param>
        /// <returns>Newly created state</returns>
        public ActionReplyMessageItem Add(ActionStatus severity, string message, params object[] args)
        {
            var retVal = Add(message, args);
            setStatus(severity);
            retVal.Severity = severity;
            return retVal;
        }

        /// <summary>
        /// Adds a message to the list.
        /// </summary>
        /// <param name="referenceKey">Field/Column/Property to identify this message (often used when implementing IDataErrorInfo)</param>
        /// <param name="severity">Severity of the message</param>
        /// <param name="message">Text to be associated with this state</param>
        /// <param name="args">Optional arguments to be supplied to message</param>
        /// <returns>Newly created state</returns>
        public ActionReplyMessageItem Add(string referenceKey, ActionStatus severity, string message, params object[] args)
        {
            var retVal = Add(message, args);
            setStatus(severity);
            retVal.Severity = severity;
            retVal.ReferenceKey = referenceKey;
            if (_status < severity)
            {
                _status = severity;
            }
            return retVal;
        }

        /// <summary>
        /// Adds a message to the list.  Default severity: information
        /// </summary>
        /// <param name="message">Text to be associated with this state. Can contain standard string.format specifiers</param>
        /// <param name="args">Arguments to be supplied to message</param>
        /// <returns>Reference to newly added state</returns>
        public ActionReplyMessageItem Add(string message, params object[] args)
        {
            ActionReplyMessageItem item = new ActionReplyMessageItem(message, args);
            item.Severity = ActionStatus.Information;
            setStatus(item.Severity);
            this.Add(item);
            return item;
        }

        /// <summary>
        /// Adds an exception to the state and sets model state to an error condition.  Default Severity: InternalError
        /// </summary>
        /// <param name="exception">Serializable exception</param>
        /// <param name="message">Text to be associated with this state. Can contain standard string.format specifiers</param>
        /// <param name="args">Arguments to be supplied to message</param>
        /// <returns>Reference to newly added state</returns>
        public ActionReplyMessageItem Add(Exception exception, string message, params object[] args)
        {
            var item = new ActionReplyMessageItem(exception, message, args);
            setStatus(item.Severity);
            this.Add(item);
            return item;

        }
        /// <summary>
        /// Adds an exception to the state and sets model state to an error condition
        /// </summary>
        /// <param name="referenceKey">Field/Column/Property to identify this message (often used when implementing IDataErrorInfo)</param>
        /// <param name="exception">Serializable exception</param>
        /// <param name="message">Text to be associated with this state. Can contain standard string.format specifiers</param>
        /// <param name="args">Arguments to be supplied to message</param>
        /// <returns>Reference to newly added state</returns>
        public ActionReplyMessageItem Add(string referenceKey, Exception exception, string message, params object[] args)
        {
            var item = new ActionReplyMessageItem(exception, message, args);
            setStatus(item.Severity);
            this.Add(item);
            item.ReferenceKey = referenceKey;
            return item;
        }

        /// <summary>
        /// Adds an exception to the state and sets model state to an error condition.  Default: ActionStatus.InternalError
        /// </summary>
        /// <param name="exception">Serializable exception</param>
        /// <returns>Reference to newly added state</returns>
        public ActionReplyMessageItem Add(Exception exception)
        {
            return Add(exception, ActionStatus.InternalError);
        }
        public ActionReplyMessageItem Add(Exception exception, ActionStatus severity)
        {
            var item = new ActionReplyMessageItem(exception);
            item.Exception = exception;
            setStatus(item.Severity);
            this.Add(item);
            return item;
        }
        /// <summary>
        /// Adds a message to this collection and recalculates maximum severity level based on newly added item
        /// </summary>
        /// <param name="item">Hydrated item to add</param>
        /// <returns>Reference to parameter item</returns>
        public new ActionReplyMessageItem Add(ActionReplyMessageItem item)
        {
            base.Add(item);
            setStatus(item.Severity);
            return item;
        }

        /// <summary>
        /// Scans all states in list.  If any state has a value &gt;=400 (ActionStatus.Error) return false
        /// </summary>
        public bool IsValid
        {
            get
            {
                return (int)MaximumSeverity < 400;
            }
        }

        /// <summary>
        /// Returns a list of error messages on object or empty list if no error messages found.  If you need message detail use <see cref="Errors"/> instead
        /// </summary>
        public List<string> ErrorMessages
        {
            get
            {
                var result = from item in Errors
                             select item.MessageText;
                if (result == null) return new List<string>();
                return result.ToList();
            }
        }

        /// <summary>
        /// Returns a list of errors on object or empty list if no errors found.
        /// </summary>
        public List<ActionReplyMessageItem> Errors
        {
            get
            {
                var result = from item in this
                             where item.IsError == true
                             select item;
                if (result == null) return new List<ActionReplyMessageItem>();
                return result.ToList();
            }
        }

        /// <summary>
        /// IDataErrorInfo. Description of what is wrong with this object.  Pipe '|' delimited concatenation of all error level messages or NULL if not found
        /// </summary>
        public string Error
        {
            get
            {
                return string.Join("|", ErrorMessages.ToArray());
            }
        }

        /// <summary>
        /// Retrieves highest (most severe) message in list.  If no messages, return null.  If more than one of
        /// a same severity, return first in list
        /// </summary>
        public ActionReplyMessageItem HighestMessage
        {
            get
            {
                if (this.Count == 0) return null;
                int maxMessageSeverity = (int)ActionStatus.Unknown;
                ActionReplyMessageItem highestMessage = null;
                foreach (var msg in this)
                {
                    if (maxMessageSeverity < (int)msg.Severity)
                    {
                        highestMessage = msg;
                        maxMessageSeverity = (int)msg.Severity;
                    }
                }
                return highestMessage;
            }
        }

        /// <summary>
        /// IDataErrorInfo.  Error on a particular field/key/column.  Null if none found
        /// </summary>
        /// <param name="columnName">Name of field/key/column to get error message for</param>
        /// <returns>First error message whose ReferenceKey matches columnName</returns>
        public string this[string columnName]
        {
            get
            {
                var result = (from item in this
                              where string.Compare(item.ReferenceKey, columnName) == 0
                              && (int)item.Severity >= 400
                              select item).FirstOrDefault();
                return (result == null) ? null : result.MessageText;
            }
        }
    }
}
