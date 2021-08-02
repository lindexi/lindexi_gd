using System.Threading.Tasks;

namespace WheburfearnofeKellehere.Contracts.Activation
{
    public interface IActivationHandler
    {
        bool CanHandle();

        Task HandleAsync();
    }
}
