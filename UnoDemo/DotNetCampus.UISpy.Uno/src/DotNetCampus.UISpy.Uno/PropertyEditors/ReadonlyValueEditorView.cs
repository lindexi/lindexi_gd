using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.UISpy.Uno.PropertyEditors;

public class ReadonlyValueEditorView : UserControl
{
    public ReadonlyValueEditorView(object initialValue)
    {
        this.DataContext(
            new ObjectEditorViewModel().WithModel(x => x.Model.Value = initialValue),
            (v, vm) => v
                .Content(new TextBlock()
                    .Text(x => x.Binding(() => vm.Value))
                ));
    }
}

public partial record ObjectEditorModel
{
    public object? Value { get; set; }
}
