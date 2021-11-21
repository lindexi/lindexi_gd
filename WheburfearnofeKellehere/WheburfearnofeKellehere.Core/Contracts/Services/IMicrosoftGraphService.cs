using System.Threading.Tasks;

using WheburfearnofeKellehere.Core.Models;

namespace WheburfearnofeKellehere.Core.Contracts.Services
{
    public interface IMicrosoftGraphService
    {
        Task<User> GetUserInfoAsync(string accessToken);

        Task<string> GetUserPhoto(string accessToken);
    }
}
