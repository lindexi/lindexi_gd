// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;

string inputCode =
    """"
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.AppendRun(new ImmutableRun("abc", textEditor.CreateRunProperty(property => property with
        {
            FontSize = 90,
            FontName = new FontName("Times New Roman"),
            FontWeight = FontWeights.Bold,
            DecorationCollection = TextEditorDecorations.Strikethrough
        })));
    }, "追加带格式的文本");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.Text = "abc";
        // 选中 'b' 这个字符
        Selection selection = new Selection(new CaretOffset(1), 1);
        RunProperty newRunProperty = textEditor.CreateRunProperty(property => property with
        {
            FontSize = 90,
            Foreground = new ImmutableBrush(Brushes.Red)
        });
        textEditor.EditAndReplaceRun(new ImmutableRun("b", newRunProperty), selection);
    }, "替换带格式文本内容");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        textEditor.Text = "abc";
        // 选中 'b' 这个字符
        Selection selection = new Selection(new CaretOffset(1), 1);
        textEditor.SetForeground(new ImmutableBrush(Brushes.Red), selection);
    }, "设置文本字符前景色");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        RunProperty styleRunProperty = textEditor.StyleRunProperty;
        textEditor.AppendRun(new ImmutableRun("a", styleRunProperty with
        {
            Foreground = new ImmutableBrush(Brushes.Red)
        }));
        textEditor.AppendRun(new ImmutableRun("b", styleRunProperty with
        {
            Foreground = new ImmutableBrush(Brushes.Green)
        }));
        textEditor.AppendRun(new ImmutableRun("c", styleRunProperty with
        {
            Foreground = new ImmutableBrush(Brushes.Blue)
        }));
    
        // 这是最全的设置文本字符属性的方式
        textEditor.ConfigRunProperty(runProperty => runProperty with
        {
            // 此方式是传入委托，将会进入多次，允许只修改某几个属性，而保留其他原本的字符属性
            // 如这里没有碰颜色属性，则依然能够保留原本字符的颜色
            FontSize = 30,
            FontName = new FontName("Times New Roman"),
        }, textEditor.GetAllDocumentSelection());
    }, "配置文本字符属性");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        RunProperty styleRunProperty = textEditor.StyleRunProperty;
        textEditor.AppendRun(new ImmutableRun("a", styleRunProperty with
        {
            Foreground = new ImmutableBrush(Brushes.Red)
        }));
        textEditor.AppendRun(new ImmutableRun("b", styleRunProperty with
        {
            Foreground = new ImmutableBrush(Brushes.Green)
        }));
        textEditor.AppendRun(new ImmutableRun("c", styleRunProperty with
        {
            Foreground = new ImmutableBrush(Brushes.Blue)
        }));
    
        // 这是最全的设置文本字符属性的方式
        RunProperty runProperty = textEditor.CreateRunProperty(runProperty => runProperty with
        {
            // 此方式是传入委托，将会进入多次，允许只修改某几个属性，而保留其他原本的字符属性
            FontSize = 30,
            FontName = new FontName("Times New Roman"),
        });
    
        // 此时会使用 runProperty 覆盖全部的文本字符属性
        textEditor.SetRunProperty(runProperty, textEditor.GetAllDocumentSelection());
    }, "设置文本字符属性");
    """";

var subCode = new StringBuilder();

var stringReader = new StringReader(inputCode);
var allLine = new List<string>();
while (true)
{
    var line = stringReader.ReadLine();
    if (line == null)
    {
        break;
    }

    allLine.Add(line);
}

var text = new StringBuilder();

for (var i = 0; i < allLine.Count; i++)
{
    var line = allLine[i];

    if (line.Contains("Add(editor =>"))
    {
        subCode.Clear();
        subCode.AppendLine(line);

        for (; i < allLine.Count; i++)
        {
            subCode.AppendLine(allLine[i]);

            if (i + 1 == allLine.Count)
            {
                break;
            }

            var nextLine = allLine[i + 1];
            if (nextLine.Contains("Add(editor =>"))
            {
                break;
            }
        }

        var code = subCode.ToString();

        // 调用提取方法
        var result = CodeProcessor.ExtractKeyInformation(code);

        /*
        ```csharp
           TextEditor textEditor = editor;
               textEditor.AppendText("abc");
               Selection selection = textEditor.GetAllDocumentSelection();
               textEditor.ToggleUnderline(selection);
        ```
         */

        static string FormatCode(string code)
        {
            var result = new StringBuilder();

            var lineList = ToLineList(code);

            var codeLineList = new List<string>();
            int? trimCount = null;

            for (var i = 0; i < lineList.Count; i++)
            {
                var line = lineList[i];

                if (line.Contains("TextEditor textEditor = editor;"))
                {
                    result.AppendLine("TextEditor textEditor = ...");
                }
                else
                {
                    codeLineList.Add(line);

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (trimCount == null)
                    {
                        trimCount = SpaceCount(line);
                    }
                    else
                    {
                        trimCount = Math.Min(SpaceCount(line), trimCount.Value);
                    }
                }
            }

            for (var i = 0; i < codeLineList.Count; i++)
            {
                var line = codeLineList[i];

                if (string.IsNullOrWhiteSpace(line))
                {
                    line = "";

                    if (i + 1 < codeLineList.Count)
                    {
                        var nextLine = codeLineList[i + 1];
                        if (nextLine.Contains("```"))
                        {
                            // 在 ``` 前不加空行
                            continue;
                        }
                    }
                }
                else if (trimCount.HasValue)
                {
                    // 去除前导空格
                    line = line.Substring(trimCount.Value);
                }

                if (i + 1 == codeLineList.Count)
                {
                    result.Append(line);
                }
                else
                {
                    result.AppendLine(line);
                }
            }

            return result.ToString();
        }

        static int SpaceCount(string line)
        {
            int count = 0;
            foreach (var c in line)
            {
                if (c == ' ')
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            return count;
        }

        // 输出结果
        text.AppendLine($"""
                         {result.Message}：

                         ```csharp
                         {FormatCode(result.Body)}
                         ```
                         
                         ---
                         
                         """);

        //Console.WriteLine($"""
        //                   {result.Message}：

        //                   ```csharp
        //                   {result.Body}
        //                   ```

        //                   ---

        //                   """);
    }
}

var message = text.ToString();
Console.WriteLine(message);

static List<string> ToLineList(string input)
{
    List<string> list = [];
    var stringReader = new StringReader(input);

    while (true)
    {
        var line = stringReader.ReadLine();
        if (line is null)
        {
            break;
        }

        list.Add(line);
    }

    return list;
}

class CodeProcessor
{
    public static (string Body, string Message) ExtractKeyInformation(string code)
    {
        // 正则表达式匹配 `Add` 方法的两个参数部分
        var match = Regex.Match(code, @"Add\s*\(\s*([a-zA-Z0-9_]+\s*=>\s*{(?<body>[\s\S]*?)})\s*,\s*""(?<message>[^""]*)""\s*\)");

        if (match.Success)
        {
            // 提取 Body 和 Message
            string body = match.Groups["body"].Value.Trim();
            string message = match.Groups["message"].Value;

            return (body, message);
        }

        throw new InvalidOperationException("无法解析输入代码，确保代码符合预期格式。");
    }
}