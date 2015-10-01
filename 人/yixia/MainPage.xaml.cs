using System;
using System.Collections.Generic;
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
using 人;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace yixia
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        viewModel view;
        public MainPage()
        {
            view = new viewModel();
            this.InitializeComponent();
            ce();
        }
        public void ce()
        {
            c_人 r = new c_人()
            {
                name_名字 = "大明" ,
                d_等级_level = 1 ,
                s_生命_lift = new ct_条("生命" , 100 , 100 , 0),
                g_攻击_strength=10,
                f_防御_firm=5,
                w_悟性_savv=0,
                q_潜力_potential=0
            };
            view.reminder = r.ToString();
        }
    }
}
