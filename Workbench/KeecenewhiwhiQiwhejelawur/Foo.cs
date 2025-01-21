using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;

namespace KeecenewhiwhiQiwhejelawur;

public class Foo
{
    
}

[GeneratedComInterface]
[Guid("3faca0d2-e7f1-4e9c-82a6-404fd6e0aab8")]
partial interface IBase
{
    void Method1(int i);
    void Method2(float i);
}

[GeneratedComInterface]
[Guid("3faca0d2-e7f1-4e9c-82a6-404fd6e0aab8")]
partial interface IDerived : IBase
{
    void Method3(long l);
    void Method4(double d);
}