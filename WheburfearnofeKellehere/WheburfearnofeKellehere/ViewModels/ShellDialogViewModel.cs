using System;
using System.Windows.Input;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace WheburfearnofeKellehere.ViewModels
{
    public class ShellDialogViewModel : ObservableObject
    {
        private ICommand _closeCommand;

        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new RelayCommand(OnClose));

        public Action<bool?> SetResult { get; set; }

        public ShellDialogViewModel()
        {
        }

        private void OnClose()
        {
            bool result = true;
            SetResult(result);
        }
    }
}
