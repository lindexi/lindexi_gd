using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp.InkDataModels;

public class InkDataModel
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point ToPoint() => new Point(X, Y);
}