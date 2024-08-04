using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhewereqanairWeelijerelall;
internal class Program
{
    static void Main(string[] args)
    {
        var element = new TopElement();

    }
}

class TopElement : IElement
{
    public TopElement()
    {
        ElementProxy = new ElementProxy(new Element());
    }

    public ElementProxy ElementProxy { set; get; }
    public void SetInput(ElementInput input)
    {
        ElementProxy.SetInput(input);
    }

    public ValueTask RunAsync()
    {
        return ElementProxy.RunAsync();
    }

    public List<ElementOutput> OutputList => ElementProxy.OutputList;

    public event EventHandler<ElementOutput>? OnOutput
    {
        add => ElementProxy.OnOutput += value;
        remove => ElementProxy.OnOutput -= value;
    }
}

record ElementInput(double Value);
record ElementOutput(double Value);

interface IElement
{
    void SetInput(ElementInput input);
    ValueTask RunAsync();

    List<ElementOutput> OutputList { get; }
    event EventHandler<ElementOutput>? OnOutput;
}

class Element : IElement
{
    public void SetInput(ElementInput input)
    {
        InputList.Add(input);
    }

    public List<ElementInput> InputList { set; get; } = new List<ElementInput>();



    public ValueTask RunAsync()
    {
        throw new NotImplementedException();
    }

    public List<ElementOutput> OutputList { get; } = new List<ElementOutput>();

    public event EventHandler<ElementOutput>? OnOutput;
}

/// <summary>
/// 由于元素可以是元素或者组合，因此需要一个中间的类型，防止元素自己变更，导致引用对象不正确
/// </summary>
class ElementProxy : IElement
{
    public ElementProxy(IElement innerElement)
    {
        InnerElement = innerElement;
    }

    public List<ElementProxy> InputElementList { get; } = new List<ElementProxy>();
    public List<ElementProxy> OutputElementList { get; } = new List<ElementProxy>();

    public IElement InnerElement { set; get; }

    public void SetInput(ElementInput input)
    {
        InnerElement.SetInput(input);
    }

    public ValueTask RunAsync()
    {
        return InnerElement.RunAsync();
    }

    public List<ElementOutput> OutputList => InnerElement.OutputList;

    public event EventHandler<ElementOutput>? OnOutput
    {
        add => InnerElement.OnOutput += value;
        remove => InnerElement.OnOutput -= value;
    }
}

class Group : IElement
{
    public List<ElementProxy> AllElementList { get; } = new List<ElementProxy>();

    public void SetInput(ElementInput input)
    {

    }

    public ValueTask RunAsync()
    {
        throw new NotImplementedException();
    }

    public List<ElementOutput> OutputList { get; }
    public event EventHandler<ElementOutput>? OnOutput;
}