using System;

using WheburfearnofeKellehere.ViewModels;

namespace WheburfearnofeKellehere.Contracts.Services
{
    public interface IUserDataService
    {
        event EventHandler<UserViewModel> UserDataUpdated;

        void Initialize();

        UserViewModel GetUser();
    }
}
