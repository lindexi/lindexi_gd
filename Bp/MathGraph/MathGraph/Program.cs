using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathGraph;
internal class Program
{
    private static void Main(string[] args)
    {
        var mathGraph = new MathGraph<int>();

        List<MathGraphElement<int>> elementList = [];

        for (int i = 0; i < 10; i++)
        {
            var mathGraphElement = mathGraph.CreateAndAddElement(i);
            elementList.Add(mathGraphElement);
        }

        for (int i = 0; i < 10; i++)
        {
            var mathGraphElement = elementList[i];
            for (int j = 0; j < 10; j++)
            {
                mathGraphElement.AddInElement(elementList[i]);
            }
        }


    }
}
