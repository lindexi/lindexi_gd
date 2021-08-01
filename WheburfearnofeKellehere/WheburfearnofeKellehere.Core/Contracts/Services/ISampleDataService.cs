using System.Collections.Generic;
using System.Threading.Tasks;

using WheburfearnofeKellehere.Core.Models;

namespace WheburfearnofeKellehere.Core.Contracts.Services
{
    public interface ISampleDataService
    {
        Task<IEnumerable<SampleOrder>> GetContentGridDataAsync();

        Task<IEnumerable<SampleOrder>> GetGridDataAsync();

        Task<IEnumerable<SampleOrder>> GetListDetailsDataAsync();
    }
}
