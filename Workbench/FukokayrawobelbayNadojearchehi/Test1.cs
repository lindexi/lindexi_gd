using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace FukokayrawobelbayNadojearchehi;

[TestClass]
public sealed class Test1
{
    [ContractTestCase]
    public void TestMethod1()
    {
        "这是单元测试内容".Test(() =>
        {

        });

        "这是单元测试内容2".Test(() =>
        {

        });
    }
}