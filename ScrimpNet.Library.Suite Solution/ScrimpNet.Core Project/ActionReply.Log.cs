using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrimpNet.Diagnostics;

namespace ScrimpNet
{
    public partial class ActionReply
    {
        private Log _log;

        public void LogWrite()
        {
            if (_log == null)
            {
                _log = Log.NewLog(this.GetType());
            }
            LogMessage logMessage = new LogMessage();
            logMessage.Severity = mapper(this.Messages.MaximumSeverity);
            ActionReplyMessageItem replyMessage = this.Messages.HighestMessage;
            if (replyMessage != null)
            {
                logMessage.MessageText = replyMessage.MessageText + " (" + replyMessage.Severity.ToString() + ") ";
                if (this.Messages.Count > 1)
                {
                    logMessage.MessageText += " 1 of " + this.Messages.Count.ToString();
                }
            }

            var sortedMessageList = Messages.OrderBy((item) => ((int)item.Severity)*-1); //sort most severe first
            int msgCount = 1;
            foreach (var msg in sortedMessageList)
            {
                string prefix = "Msg" + msgCount.ToString("00");
                logMessage.ExtendedProperties.Add(prefix + "[Severity]", msg.Severity.ToString());
                if (string.IsNullOrWhiteSpace(msg.ReferenceKey) == false)
                {
                    logMessage.ExtendedProperties.Add(prefix+"[Reference]", msg.ReferenceKey.ToString());
                }
                logMessage.ExtendedProperties.Add(prefix+"[Text]", msg.MessageText.ToString());
                if (msg.Exception != null)
                {
                    logMessage.ExtendedProperties.Add(prefix+"[Exception]", msg.Exception.Expand());
                }
                msgCount++;
            }
            
            //EXTENSION serialize DataItems and Value object and add them to extendend properties

            _log.Write(logMessage);
        }

        private MessageLevel mapper(ActionStatus actionStatus)
        {
            switch (actionStatus)
            {
                case ActionStatus.Error:
                case ActionStatus.Conflict:
                case ActionStatus.AuthenticationRequired:
                case ActionStatus.Forbidden:
                case ActionStatus.NotFound:
                case ActionStatus.RequestTimeOut: return MessageLevel.Error;
                case ActionStatus.InternalError: return MessageLevel.Critical;
                case ActionStatus.OK: return MessageLevel.Information;
                case ActionStatus.Unknown: return MessageLevel.Warning;
                case ActionStatus.NoContent: return MessageLevel.Information;
                default:
                    throw ExceptionFactory.New<ArgumentException>("Unable to handle ActionStatus.{0}", actionStatus);
            }
        }
    }

    public partial class ActionReply<T>
    {
        public void LogWrite()
        {
            base.LogWrite();
        }
    }
}
