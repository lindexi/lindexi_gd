using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WalqujemjelNekokelhuwererere;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
    }
}
