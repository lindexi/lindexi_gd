using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWrite.Business;

public interface ICopilotHandler
{
    Task SendMessageToCopilotAsync(string text);
}
