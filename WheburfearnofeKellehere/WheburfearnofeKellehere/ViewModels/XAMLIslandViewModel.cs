using System;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace WheburfearnofeKellehere.ViewModels
{
    public class XAMLIslandViewModel : ObservableObject
    {
        private string _text;

        public string Text
        {
            get { return _text; }
            set { SetProperty(ref _text, value); }
        }

        public XAMLIslandViewModel()
        {
        }
    }
}
