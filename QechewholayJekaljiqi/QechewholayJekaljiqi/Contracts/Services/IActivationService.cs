using System.Threading.Tasks;

namespace QechewholayJekaljiqi.Contracts.Services
{
    public interface IActivationService
    {
        Task ActivateAsync(object activationArgs);
    }
}
