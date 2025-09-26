using System.Text;

namespace LightTextEditorPlus.Tests.IntegrationTests;

public class IntegrationTestException : AggregateException
{
    public IntegrationTestException(List<(string Name, Exception Exception)> exceptionList) : base(exceptionList.Select(t => t.Exception))
    {
        _exceptionList = exceptionList;
    }

    private readonly List<(string Name, Exception Exception)> _exceptionList;

    public override string Message => ToText();

    public override string ToString()
    {
        return ToText();
    }

    private string ToText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("IntegrationTest Fail!");
        stringBuilder.AppendLine($"Current Image Output Folder: {IntegrationTest.CurrentTestFolder}");

        stringBuilder.AppendLine("==========");

        foreach ((string name, Exception exception) in _exceptionList)
        {
            stringBuilder.AppendLine($"[IntegrationTest] Fail {name}")
                .AppendLine(exception.ToString());

            stringBuilder.AppendLine("==========");
        }

        return stringBuilder.ToString();
    }
}