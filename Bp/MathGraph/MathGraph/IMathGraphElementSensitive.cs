namespace MathGraph;

public interface IMathGraphElementSensitive<T>
{
    MathGraphElement<T> MathGraphElement { set; get; }
}