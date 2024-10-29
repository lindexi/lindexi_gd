public static class Program
{
    public static void Main()
    {
        object array = new uint[] { 1, 2 };

        AnalyzeType(array);
    }
    public static void AnalyzeType(object o)
    {
        switch (o)
        {
            case IEnumerable<int> value:
                Console.WriteLine("Int array"); break;
            case IEnumerable<uint> value:
                Console.WriteLine("UInt array"); break;
            default:
                Console.WriteLine("Unknown type"); break;
        }
    }
}