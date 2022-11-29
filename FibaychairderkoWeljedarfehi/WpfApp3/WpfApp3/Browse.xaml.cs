using System.Windows.Input;
using Prism.Commands;

namespace WpfApp3;

public partial class Browse
{
    public Browse()
    {
        NavigateLeftCommand = new DelegateCommand(NavigateLeftCommandAction);
        NavigateRightCommand = new DelegateCommand(NavigateRightCommandAction);
        InitializeComponent();
    }

    public DelegateCommand NavigateLeftCommand { get; set; }
    public DelegateCommand NavigateRightCommand { get; set; }

    private void NavigateLeftCommandAction()
    {
    }

    private void NavigateRightCommandAction()
    {
    }
}