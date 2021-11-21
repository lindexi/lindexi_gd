using System;
using System.ServiceModel;
using System.Threading;

namespace ChigiwejefiKemhakerhawee
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataService = new DataService();
            Uri address = new Uri("net.pipe://localhost/MyWCFConnection");
            using (ServiceHost host = new ServiceHost(dataService, address))
            {
                host.Open();

                while (true)
                {
                    Thread.Sleep(100);
                }
            }


        }
    }
}
