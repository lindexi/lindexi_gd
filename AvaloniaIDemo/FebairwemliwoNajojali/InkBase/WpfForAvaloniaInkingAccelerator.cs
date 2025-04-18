using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkBase;

public class WpfForAvaloniaInkingAccelerator
{
    public static WpfForAvaloniaInkingAccelerator Instance { get; } = new WpfForAvaloniaInkingAccelerator();

    public IWpfInkLayer InkLayer { get; set; } = null!;
}