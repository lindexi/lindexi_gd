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
        var element = new Element();

    }
}

record ElementInput();
record ElementOutput();

interface IElement
{
    void SetInput(ElementInput input);
    ValueTask<ElementOutput> RunAsync();
}

class Element : IElement
{
    public void SetInput(ElementInput input)
    {
        
    }

    public ValueTask<ElementOutput> RunAsync()
    {
        throw new NotImplementedException();
    }
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

    public ValueTask<ElementOutput> RunAsync()
    {
        return InnerElement.RunAsync();
    }
}

class Group : IElement
{
    public List<ElementProxy> AllElementList { get; } = new List<ElementProxy>();

    public void SetInput(ElementInput input)
    {
        
    }

    public ValueTask<ElementOutput> RunAsync()
    {
        throw new NotImplementedException();
    }
}