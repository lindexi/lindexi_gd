using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ScrimpNet.Text;

namespace ScrimpNet.Web
{
    /// <summary>
    /// Describes a single state item for an action response.  A response might have more than one message returning to the caller
    /// </summary>
    [DataContract]
    public class MessageItem : ICloneable
    {

        /// <summary>
        /// Level of importance this state is to the creator of the state.
        /// </summary>
        [DataMember]
        public ActionStatus Severity { get; set; }

        /// <summary>
        /// Any text application wants to associate with this state
        /// </summary>
        [DataMember]
        public string MessageText { get; set; }

        /// <summary>
        /// Used to store field/property names when implementing IDataErrorInfo.  Can be any user provided value
        /// </summary>
        [DataMember]
        public string ReferenceKey { get; set; }


        /// <summary>
        /// Optional .Net native exception that should be associated with this state.
        /// </summary>
        /// <remarks>User should make sure Exception is serializable if passing state across application boundries</remarks>
        [DataMember]
        public Exception Exception { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MessageItem()
        {
            Severity = ActionStatus.Unknown;
        }

        /// <summary>
        /// Creates an state message.  Default state: Information
        /// </summary>
        /// <param name="messageText">Text to associate with this state</param>
        /// <param name="args">Arguments to supply ot this text</param>
        public MessageItem(string messageText, params object[] args)
            : this()
        {
            Severity = ActionStatus.Information;
            MessageText = TextUtils.StringFormat(messageText, args);
        }

        /// <summary>
        /// Creates a state message
        /// </summary>
        /// <param name="severity">The type of message (info, error, etc)</param>
        /// <param name="messageText">Text to associate with this state</param>
        /// <param name="args">Arguments to supply ot this text</param>
        public MessageItem(ActionStatus severity, string messageText, params object[] args)
        {
            Severity = severity;
            MessageText = TextUtils.StringFormat(messageText, args);
        }

        /// <summary>
        /// Creates a state message
        /// </summary>
        /// <param name="referenceKey">Field/Property level name (used for IDataErrorInfo bindings)</param>
        /// <param name="severity">The type of message (info, error, etc)</param>
        /// <param name="messageText">Text to associate with this state</param>
        /// <param name="args">Arguments to supply ot this text</param>
        public MessageItem( string referenceKey, ActionStatus severity, string messageText, params object[] args)
        {
            Severity = severity;
            ReferenceKey = referenceKey;
            MessageText = TextUtils.StringFormat(messageText, args);
        }

        /// <summary>
        /// Creates an ERROR level message.  MessageText = ex.Message
        /// </summary>
        /// <param name="ex">Exception to associate with this message.  Should be serializable if state is going to be sent across application boundries</param>
        public MessageItem(Exception ex):this(ex, ActionStatus.InternalError)
        {
            
        }

        public MessageItem(Exception ex, ActionStatus severity)
        {
            Exception = ex;
            MessageText = ex.Message;
            Severity = severity;
        }
        /// <summary>
        /// Creates an ERROR level message.  MessageText = ex.Message
        /// </summary>
        /// <param name="referenceKey">Field/Property level name (used for IDataErrorInfo bindings)</param>
        /// <param name="ex">Exception to associate with this message.  Should be serializable if state is going to be sent across application boundries</param>
        public MessageItem(string referenceKey, Exception ex)
        {
            Exception = ex;
            if (ex != null)
            {
                MessageText = ex.Message;
            }
            Severity = ActionStatus.InternalError;
            ReferenceKey = referenceKey;
        }


        /// <summary>
        /// Creates an ERROR level message
        /// </summary>
        /// <param name="ex">Exception to associate with this message.  Should be serializable if state is going to be sent across application boundries</param>
        /// <param name="messageText">Text to associate with this state</param>
        /// <param name="args">Arguments to supply ot this text</param>
        public MessageItem(Exception ex, string messageText, params object[] args)
        {
            Exception = ex;
            MessageText = TextUtils.StringFormat(messageText, args);            
            Severity = ActionStatus.InternalError;
        }

        /// <summary>
        /// Creates an ERROR level message
        /// </summary>
        /// <param name="referenceKey">Field/Property level name (used for IDataErrorInfo bindings)</param>
        /// <param name="ex">Exception to associate with this message.  Should be serializable if state is going to be sent across application boundries</param>
        /// <param name="messageText">Text to associate with this state</param>
        /// <param name="args">Arguments to supply ot this text</param>
        public MessageItem(string referenceKey, Exception ex, string messageText, params object[] args)
        {
            Exception = ex;
            MessageText = TextUtils.StringFormat(messageText, args);
            Severity = ActionStatus.InternalError;
            ReferenceKey = referenceKey;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="item"></param>
        public MessageItem(MessageItem item)
        {
            Exception = item.Exception;
            Severity = item.Severity;
            MessageText = item.MessageText;
            ReferenceKey = item.ReferenceKey;
        }

        /// <summary>
        /// Any error code greater than 400
        /// </summary>
        public bool IsError
        {
            get
            {
                return (int)Severity >= 400; //any severity greater than 400 are errors (see HTTP standard response codes)
            }
        }

        /// <summary>
        /// Provides a copy of this object.  
        /// </summary>
        /// <returns>Copy of this object (except for any attached exceptions.  Exceptions are attached by reference not by value</returns>
        public object Clone()
        {
            return new MessageItem(
                 Severity = this.Severity,
                 MessageText = this.MessageText,
                 ReferenceKey = this.ReferenceKey,
                 Exception = this.Exception //not really a 'clone' but it works for now
            );
        }
    }

}
