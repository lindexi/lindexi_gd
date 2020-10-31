using dotnetCampus.Ipc.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Ipc.Tests
{
    [TestClass]
    public class IpcObjectJsonSerializerTests
    {
        [ContractTestCase]
        public void Serialize()
        {
            "序列化对象之后，能否通过二进制放序列化回对象".Test(() =>
            {
                // Arrange
                IIpcObjectSerializer ipcObjectSerializer = new IpcObjectJsonSerializer();
                var foo = new Foo()
                {
                    Name = "林德熙是逗比"
                };

                // Action
                var byteList = ipcObjectSerializer.Serialize(foo);
                var deserializeFoo = ipcObjectSerializer.Deserialize<Foo>(byteList);

                // Assert
                Assert.AreEqual(foo.Name, deserializeFoo.Name);
            });
        }

        public class Foo
        {
            public string Name { set; get; } = "";
        }
    }
}
