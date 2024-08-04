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
}


class Element : IElement
{
}

class Group : IElement
{
}