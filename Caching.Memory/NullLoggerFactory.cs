using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Caching.Memory
{
    public class NullLoggerFactory
    {
        static void Main(string[] args)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(MyClass));
        }

        public static ILoggerFactory Instance { get; set; }
    }

    public class MyClass
    {
        public int Value;
    }
}