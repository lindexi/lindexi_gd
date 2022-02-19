// See https://aka.ms/new-console-template for more information

A a = new A();
object o = a;
B b = a;
b = (B) o;

class A
{
    public static implicit operator B(A a)
    {
        return new B();
    }
}

class B
{

}