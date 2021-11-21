using System;
using System.Collections.ObjectModel;
using System.Linq;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

using WheburfearnofeKellehere.Contracts.ViewModels;
using WheburfearnofeKellehere.Core.Contracts.Services;
using WheburfearnofeKellehere.Core.Models;

namespace WheburfearnofeKellehere.ViewModels
{
    public class ListDetailsViewModel : ObservableObject, INavigationAware
    {
        private readonly ISampleDataService _sampleDataService;
        private SampleOrder _selected;

        public SampleOrder Selected
        {
            get { return _selected; }
            set { SetProperty(ref _selected, value); }
        }

        public ObservableCollection<SampleOrder> SampleItems { get; private set; } = new ObservableCollection<SampleOrder>();

        public ListDetailsViewModel(ISampleDataService sampleDataService)
        {
            _sampleDataService = sampleDataService;
        }

        public async void OnNavigatedTo(object parameter)
        {
            SampleItems.Clear();

            var data = await _sampleDataService.GetListDetailsDataAsync();

            foreach (var item in data)
            {
                SampleItems.Add(item);
            }

            Selected = SampleItems.First();
        }

        public void OnNavigatedFrom()
        {
        }
    }
}
