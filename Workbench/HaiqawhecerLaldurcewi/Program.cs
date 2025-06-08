// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;

string inputCode =
    """"
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.AppendText("abc");
        Selection selection = textEditor.GetAllDocumentSelection();
        textEditor.ToggleUnderline(selection);
    }, "开启或关闭下划线");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.AppendText("abc");
        Selection selection = textEditor.GetAllDocumentSelection();
        textEditor.ToggleStrikethrough(selection);
    }, "开启或关闭删除线");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.AppendText("abc");
        Selection selection = textEditor.GetAllDocumentSelection();
        textEditor.ToggleEmphasisDots(selection);
    }, "开启或关闭着重号");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.AppendText("x2");
        Selection selection = new Selection(new CaretOffset(1), 1);
        textEditor.ToggleSuperscript(selection);
    }, "开启或关闭上标");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.AppendText("x2");
        Selection selection = new Selection(new CaretOffset(1), 1);
        textEditor.ToggleSubscript(selection);
    }, "开启或关闭下标");
    
    // 段落属性
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.Text = "Text";
        // 水平居中是段落属性的
        textEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Center
        });
    }, "设置文本水平居中");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.Text = "Text";
        // 水平居右是段落属性的
        textEditor.ConfigCurrentCaretOffsetParagraphProperty(property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Right
        });
    }, "设置文本水平居右");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.Text = """
                          aaa
                          bbb
                          ccc
                          """;
    
        textEditor.ConfigParagraphProperty(new ParagraphIndex(1), property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Center
        });
        textEditor.ConfigParagraphProperty(new ParagraphIndex(2), property => property with
        {
            HorizontalTextAlignment = HorizontalTextAlignment.Right
        });
    }, "设置指定段落属性");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
        textEditor.Text = """
                          aaa
                          bbb
                          ccc
                          """;
    
        textEditor.ConfigParagraphProperty(new ParagraphIndex(2), property => property with
        {
            LineSpacing = new MultipleTextLineSpace(2)
        });
    }, "设置两倍行距");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        textEditor.Text = new string(Enumerable.Repeat('a', 100).ToArray());
    
        textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
        {
            Indent = 50,
            IndentType = IndentType.FirstLine,
        });
    }, "设置段落首行缩进");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        textEditor.Text = new string(Enumerable.Repeat('a', 100).ToArray());
        textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
        textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
        {
            Indent = 200,
            IndentType = IndentType.Hanging,
        });
    }, "设置段落悬挂缩进");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        // 制作两段，方便查看效果
        string text = new string(Enumerable.Repeat('a', 100).ToArray());
        textEditor.Text = text;
        textEditor.AppendText("\n" + text);
    
        textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
        textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
        {
            LeftIndentation = 100
        });
    }, "设置段落左侧缩进");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        // 制作两段，方便查看效果
        string text = new string(Enumerable.Repeat('a', 100).ToArray());
        textEditor.Text = text;
        textEditor.AppendText("\n" + text);
    
        textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
        textEditor.ConfigCurrentCaretOffsetParagraphProperty(paragraphProperty => paragraphProperty with
        {
            RightIndentation = 100
        });
    }, "设置段落右侧缩进");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        // 制作三段，方便查看效果
        string text = new string(Enumerable.Repeat('a', 100).ToArray());
        textEditor.Text = text;
        textEditor.AppendText("\n" + text + "\n" + text);
    
        textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
        textEditor.ConfigParagraphProperty(new ParagraphIndex(1), paragraphProperty => paragraphProperty with
        {
            ParagraphBefore = 100
        });
    }, "设置段前间距");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        // 制作三段，方便查看效果
        string text = new string(Enumerable.Repeat('a', 100).ToArray());
        textEditor.Text = text;
        textEditor.AppendText("\n" + text + "\n" + text);
    
        textEditor.SetFontSize(20, textEditor.GetAllDocumentSelection());
        textEditor.ConfigParagraphProperty(new ParagraphIndex(1), paragraphProperty => paragraphProperty with
        {
            ParagraphAfter = 100
        });
    }, "设置段后间距");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        textEditor.AppendText("a\nb\nc");
        for (var i = 0; i < textEditor.ParagraphList.Count; i++)
        {
            textEditor.ConfigParagraphProperty(new ParagraphIndex(i), paragraphProperty => paragraphProperty with
            {
                Marker = new BulletMarker()
                {
                    MarkerText = "l",
                    RunProperty = textEditor.CreateRunProperty(runProperty => runProperty with
                    {
                        FontName = new FontName("Wingdings"),
                        FontSize = 15,
                    })
                }
            });
        }
    }, "设置无序项目符号");
    
    Add(editor =>
    {
        TextEditor textEditor = editor;
    
        textEditor.AppendText("a\nb\nc");
        var numberMarkerGroupId = new NumberMarkerGroupId();
        for (var i = 0; i < textEditor.ParagraphList.Count; i++)
        {
            textEditor.ConfigParagraphProperty(new ParagraphIndex(i), paragraphProperty =>
            {
                return paragraphProperty with
                {
                    Marker = new NumberMarker()
                    {
                        GroupId = numberMarkerGroupId
                    }
                };
            });
        }
    }, "设置有序项目符号");
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