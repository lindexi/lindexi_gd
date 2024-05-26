// See https://aka.ms/new-console-template for more information

using System.CodeDom.Compiler;

using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

using UnoSpySnoopDebugger.IpcCommunicationContext;

var ipcProvider = new JsonIpcDirectRoutedProvider();
var client = await ipcProvider.GetAndConnectClientAsync("UnoSpySnoop");

var elementProxy = await client.GetResponseAsync<ElementProxy>(RoutedPathList.GetRootVisualTree)!;

var stringWriter = new StringWriter();
var indentedTextWriter = new IndentedTextWriter(stringWriter, "  ");
WriteElement(elementProxy);
void WriteElement(ElementProxy element)
{
    if (!string.IsNullOrEmpty(element.ElementInfo.ElementName))
    {
        indentedTextWriter.Write($"{element.ElementInfo.ElementName} (element.ElementInfo.ElementType)");
    }
    else
    {
        indentedTextWriter.Write(element.ElementInfo.ElementTypeName);
    }
    indentedTextWriter.WriteLine();

    if (element.Children != null)
    {
        indentedTextWriter.Indent++;
        foreach (var child in element.Children)
        {
            WriteElement(child);
        }

        indentedTextWriter.Indent--;
    }
}

Console.WriteLine(stringWriter.ToString());

Console.Read();
