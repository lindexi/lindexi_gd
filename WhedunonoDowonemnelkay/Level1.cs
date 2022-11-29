using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2;

public class Level1
{
    public string DisplayText { get; set; } = "";

    public ObservableCollection<Level2> Children { get; } = new();
}
