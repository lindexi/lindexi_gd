using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetCampus.UISpy.Uno.Tree;

namespace DotNetCampus.UISpy.Uno.Models;

public partial record ElementProxyTreeModel
{
    public IState<UIElement> CurrentRootElement => State<UIElement>.Empty(this);

    public IFeed<ElementProxyTree> CurrentTree => Feed.Async(UpdateVisualTree);

    public void SetRootElement(UIElement rootElement)
    {
    }

    private ValueTask<ElementProxyTree> UpdateVisualTree(CancellationToken ct)
    {
        return ValueTask.FromResult<ElementProxyTree>(default(ElementProxyTree));
    }
}

