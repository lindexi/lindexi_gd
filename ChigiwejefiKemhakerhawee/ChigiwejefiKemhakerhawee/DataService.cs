using System;
using System.ServiceModel;

namespace ChigiwejefiKemhakerhawee
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class DataService: IDataServer
    {
        public void Foo(string name)
        {
            Console.WriteLine(name);
        }
    }

    [ServiceContract]
    public interface IDataServer
    {
        [OperationContract]
        void Foo(string name);
    }
}