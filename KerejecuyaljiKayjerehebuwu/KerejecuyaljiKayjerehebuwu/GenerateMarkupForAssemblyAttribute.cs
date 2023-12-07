using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Generator;

[AttributeUsage(AttributeTargets.Assembly)]
internal class GenerateMarkupForAssemblyAttribute : Attribute
{
    public GenerateMarkupForAssemblyAttribute(Type type)
    {
    }
}
