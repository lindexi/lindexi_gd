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

class Group : IElement
{
    public void SetInput(ElementInput input)
    {
        
    }

    public ValueTask<ElementOutput> RunAsync()
    {
        throw new NotImplementedException();
    }
}