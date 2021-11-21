using System;
using System.Threading.Tasks;

using WheburfearnofeKellehere.Core.Helpers;

namespace WheburfearnofeKellehere.Core.Contracts.Services
{
    public interface IIdentityService
    {
        event EventHandler LoggedIn;

        event EventHandler LoggedOut;

        void InitializeWithAadAndPersonalMsAccounts(string clientId, string redirectUri = null);

        void InitializeWithPersonalMsAccounts(string clientId, string redirectUri = null);

        void InitializeWithAadMultipleOrgs(string clientId, bool integratedAuth = false, string redirectUri = null);

        void InitializeWithAadSingleOrg(string clientId, string tenant, bool integratedAuth = false, string redirectUri = null);

        bool IsLoggedIn();

        Task<LoginResultType> LoginAsync();

        bool IsAuthorized();

        string GetAccountUserName();

        Task LogoutAsync();

        Task<string> GetAccessTokenForGraphAsync();

        Task<string> GetAccessTokenAsync(string[] scopes);

        Task<bool> AcquireTokenSilentAsync();
    }
}
