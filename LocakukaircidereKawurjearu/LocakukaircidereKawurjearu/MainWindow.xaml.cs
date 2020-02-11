using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using MvvmDialogs;
using MvvmDialogs.FrameworkDialogs.MessageBox;

namespace LocakukaircidereKawurjearu
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainWindowViewModel>();
        }

        public MainWindowViewModel MainWindow => ServiceLocator.Current.GetInstance<MainWindowViewModel>();
    }

    public class CustomMessageBox : IMessageBox
    {
        private readonly MessageBoxSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMessageBox"/> class.
        /// </summary>
        /// <param name="settings">The settings for the folder browser dialog.</param>
        public CustomMessageBox(MessageBoxSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        

            SetUpTitle();
            SetUpButtons();
            SetUpIcon();
        }

        /// <summary>
        /// Opens a message box with specified owner.
        /// </summary>
        /// <param name="owner">
        /// Handle to the window that owns the dialog.
        /// </param>
        /// <returns>
        /// A <see cref="MessageBoxResult"/> value that specifies which message box button is
        /// clicked by the user.
        /// </returns>
        public MessageBoxResult Show(Window owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            return new MessageBoxResult();
        }

        private void SetUpTitle()
        {
            
        }

        private void SetUpButtons()
        {
           
        }

        private void SetUpIcon()
        {
          
        }

       
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IDialogService dialogService;

        private string confirmation;

        public MainWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            ShowMessageBoxWithMessageCommand = new RelayCommand(ShowMessageBoxWithMessage);
            ShowMessageBoxWithCaptionCommand = new RelayCommand(ShowMessageBoxWithCaption);
            ShowMessageBoxWithButtonCommand = new RelayCommand(ShowMessageBoxWithButton);
            ShowMessageBoxWithIconCommand = new RelayCommand(ShowMessageBoxWithIcon);
            ShowMessageBoxWithDefaultResultCommand = new RelayCommand(ShowMessageBoxWithDefaultResult);
        }

        public ICommand ShowMessageBoxWithMessageCommand { get; }

        public ICommand ShowMessageBoxWithCaptionCommand { get; }

        public ICommand ShowMessageBoxWithButtonCommand { get; }

        public ICommand ShowMessageBoxWithIconCommand { get; }

        public ICommand ShowMessageBoxWithDefaultResultCommand { get; }

        public string Confirmation
        {
            get => confirmation;
            private set { Set(() => Confirmation, ref confirmation, value); }
        }

        private void ShowMessageBoxWithMessage()
        {
            MessageBoxResult result = dialogService.ShowMessageBox(
                this,
                "This is the text.");

            UpdateResult(result);
        }

        private void ShowMessageBoxWithCaption()
        {
            MessageBoxResult result = dialogService.ShowMessageBox(
                this,
                "This is the text.",
                "This Is The Caption");

            UpdateResult(result);
        }

        private void ShowMessageBoxWithButton()
        {
            MessageBoxResult result = dialogService.ShowMessageBox(
                this,
                "This is the text.",
                "This Is The Caption",
                MessageBoxButton.OKCancel);

            UpdateResult(result);
        }

        private void ShowMessageBoxWithIcon()
        {
            MessageBoxResult result = dialogService.ShowMessageBox(
                this,
                "This is the text.",
                "This Is The Caption",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information);

            UpdateResult(result);
        }

        private void ShowMessageBoxWithDefaultResult()
        {
            MessageBoxResult result = dialogService.ShowMessageBox(
                this,
                "This is the text.",
                "This Is The Caption",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information,
                MessageBoxResult.Cancel);

            UpdateResult(result);
        }

        private void UpdateResult(MessageBoxResult result)
        {
            switch (result)
            {
                case MessageBoxResult.OK:
                    Confirmation = "We got confirmation to continue!";
                    break;

                case MessageBoxResult.Cancel:
                    Confirmation = string.Empty;
                    break;

                default:
                    throw new NotSupportedException($"{confirmation} is not supported.");
            }
        }
    }
}
