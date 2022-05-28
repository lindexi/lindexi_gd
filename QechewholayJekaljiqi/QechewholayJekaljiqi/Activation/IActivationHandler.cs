using System.Threading.Tasks;

namespace QechewholayJekaljiqi.Activation
{
    public interface IActivationHandler
    {
        bool CanHandle(object args);

        Task HandleAsync(object args);
    }
}
