using System;
using System.Linq;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

using WheburfearnofeKellehere.Contracts.ViewModels;
using WheburfearnofeKellehere.Core.Contracts.Services;
using WheburfearnofeKellehere.Core.Models;

namespace WheburfearnofeKellehere.ViewModels
{
    public class ContentGridDetailViewModel : ObservableObject, INavigationAware
    {
        private readonly ISampleDataService _sampleDataService;
        private SampleOrder _item;

        public SampleOrder Item
        {
            get { return _item; }
            set { SetProperty(ref _item, value); }
        }

        public ContentGridDetailViewModel(ISampleDataService sampleDataService)
        {
            _sampleDataService = sampleDataService;
        }

        public async void OnNavigatedTo(object parameter)
        {
            if (parameter is long orderID)
            {
                var data = await _sampleDataService.GetContentGridDataAsync();
                Item = data.First(i => i.OrderID == orderID);
            }
        }

        public void OnNavigatedFrom()
        {
        }
    }
}
