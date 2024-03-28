<<<<<<< HEAD
﻿using System.Text;
=======
﻿using System.Collections;
using System.Reflection;
using System.Text;
>>>>>>> 5c8a31243b7f2e1ad87f49b319dbab39d5d18f0e
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LunallherbeanalLerejucahallyeler;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
<<<<<<< HEAD
=======
        foreach (var temp in new IBase[]
                 {
                   new Type0(),
                   new Type1(),
                   new Type2(),
                   new Type3(),
                   new Type4(),
                   new Type5(),
                   new Type6(),
                   new Type7(),
                   new Type8(),
                   new Type9(),
                 })
        {
            temp.Add();
        }

        var propertyFromNameField = typeof(DependencyProperty).GetField("PropertyFromName", BindingFlags.Static | BindingFlags.NonPublic);
        var propertyFromName = (Hashtable) propertyFromNameField.GetValue(null);
        TextBlock.Text = $"依赖属性定义数量：{propertyFromName.Count}";
    }
}

public interface IBase
{
    void Add();
}

public partial class Type1 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}

public partial class Type2 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}

public partial class Type3 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}

public partial class Type0 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}

public partial class Type5 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}
public partial class Type4 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}
public partial class Type6 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}
public partial class Type7 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}
public partial class Type8 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
    }
}
public partial class Type9 : DependencyObject, IBase
{
    private static int _count = 0;
    public void Add()
    {
        // 写入静态字段只是为了触发静态构造函数
        _count++;
>>>>>>> 5c8a31243b7f2e1ad87f49b319dbab39d5d18f0e
    }
}