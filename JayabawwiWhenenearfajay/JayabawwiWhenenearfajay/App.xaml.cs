using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;

namespace JayabawwiWhenenearfajay;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 注释这句话试试
        var value = ((System.Windows.Setter) (Resources.MergedDictionaries)[1].Values.OfType<Style>().First().Setters[0]).Value;

        var resourceDictionary = new ResourceDictionary()
        {
            Source = new Uri("/Dictionary1.xaml", UriKind.RelativeOrAbsolute)
        };

        Resources.MergedDictionaries.Add(resourceDictionary);

        base.OnStartup(e);
    }
}