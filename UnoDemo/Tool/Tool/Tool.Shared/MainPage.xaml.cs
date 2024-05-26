using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Tool.Shared.Framework;
using Tool.Shared.Model;
using Tool.Shared.View;
using Tool.Shared.ViewModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Tool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }


        public MainModel ViewModel { get; } = new MainModel();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel.OnNavigate += ViewModel_OnNavigate;

            ViewModel.OnNavigatedTo(ViewModelPageBind);
        }

        public ViewModelPageBind ViewModelPageBind { get; } = new ViewModelPageBind
        (
            new (PageModel name, Type page, Func<IViewModel> createViewModel)[]
            {
                (new PageModel(nameof(NavigatePage)), typeof(NavigatePage), () => new NavigateModel()),
                (new PageModel("JsonPropertyConvertPage")
                {
                    Describe = "Json 属性大小写转换"
                }, typeof(JsonPropertyConvertPage), () => new JsonPropertyConvertModel()),
                (new PageModel("PptValueConvertPage")
                {
                    Describe = "PPT 单位转换"
                }, typeof(PptValueConvertPage), () => new PptValueConvertModel()),
            }
        );

        private void ViewModel_OnNavigate(object sender, NavigationPage e)
        {
            var page = ViewModelPageBind.GetPageType(e.PageName);
            MainFrame.Navigate(page, e.Parameter.ViewModel);
        }
    }
}