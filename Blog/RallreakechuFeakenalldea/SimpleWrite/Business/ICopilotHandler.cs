using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWrite.Business;

public interface ICopilotHandler
{
    /// <summary>
    /// 发送消息到 Copilot 聊天
    /// </summary>
    /// <param name="text"></param>
    /// <param name="withHistory"></param>
    /// <returns></returns>
    Task SendMessageToCopilotAsync(string text, bool withHistory);
}
