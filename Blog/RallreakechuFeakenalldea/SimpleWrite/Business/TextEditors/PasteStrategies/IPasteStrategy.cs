using System.Threading.Tasks;

namespace SimpleWrite.Business.TextEditors.PasteStrategies;

internal interface IPasteStrategy
{
    Task<bool> PasteAsync(PasteContext context);
}