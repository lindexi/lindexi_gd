using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2;

public class Root
{
    public string DisplayText { get; set; } = "";

    public ObservableCollection<Level1> Children { get; } = new();
}
