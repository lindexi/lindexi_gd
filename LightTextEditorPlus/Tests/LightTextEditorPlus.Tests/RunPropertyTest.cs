using dotnetCampus.UITest.WPF;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class RunPropertyTest
{
    [UIContractTestCase]
    public void Equals()
    {
        "获取的两个默认的 RunProperty 对象，判断相等，返回相等".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            // 获取的两个默认的 RunProperty 对象
            var runProperty1 = runPropertyCreator.GetDefaultRunProperty();
            var runProperty2 = runPropertyCreator.GetDefaultRunProperty();

            // 判断相等，返回相等
            var equals = runProperty1.Equals(runProperty2);
            Assert.AreEqual(true,equals);
        });
    }
}