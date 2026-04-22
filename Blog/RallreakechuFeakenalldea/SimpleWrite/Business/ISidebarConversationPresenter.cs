using System.Threading.Tasks;

namespace SimpleWrite.Business;

internal interface ISidebarConversationPresenter
{
    Task ShowConversationAsync(string userText, string assistantText);
}
