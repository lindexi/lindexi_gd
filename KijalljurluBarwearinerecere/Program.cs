using System;
using System.ServiceModel;
using ChigiwejefiKemhakerhawee;

namespace KijalljurluBarwearinerecere
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri address = new Uri("net.pipe://localhost/MyWCFConnection");

            var dataServer = ChannelFactory<IDataServer>.CreateChannel(new NetNamedPipeBinding(),new EndpointAddress(address));
            dataServer.Foo("123");
        }
    }
}
