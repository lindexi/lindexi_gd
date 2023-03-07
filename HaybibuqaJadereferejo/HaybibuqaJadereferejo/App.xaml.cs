using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HaybibuqaJadereferejo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // 好孩子可不要在这里写相对路径哦
        var logFile = "log.txt";
        // 将相对路径转换为绝对路径，这样要是写错地方了，在这里可以快速调试到
        logFile = Path.GetFullPath(logFile);
        var logWriter = new LogWriter(logFile, Console.Out)
        {
            AutoFlush = true,
        };

        // 以上的方法将会在每次打开应用时，清空原有的日志文件。如果只是期望追加的话，可以使用下面的代码
        //var fileStream = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        //logWriter = new LogWriter(fileStream)
        //{
        //    AutoFlush = true,
        //};
        Console.SetOut(logWriter);



        LogWriter = logWriter;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        LogWriter.Dispose();
    }

    private LogWriter LogWriter { get; }
}

class LogWriter : StreamWriter
{

    public LogWriter(string path, TextWriter textWriter) : base(path)
    {
        _textWriter = textWriter;
    }

    private readonly TextWriter _textWriter;

    public override void WriteLine(string? value)
    {
        // 可以在这里对输出的字符串进行处理，例如加上时间
        var message = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff") + "] " + value;
        base.WriteLine(message);

        Task.Run(() =>
        {
            _textWriter.WriteLine(message);
        });
        Debugger.Log(0, null, message + "\r\n");
    }
}


abstract class 书籍管理基类
{
    public void 总工序()
    {
        工序0();
        工序1();
        工序2();
        工序3();
    }

    protected abstract void 工序0();

    private void 工序1()
    {
        // ...
    }
    protected abstract void 工序2();

    private void 工序3()
    {
        // ...
    }
}

class 人文书籍管理 : 书籍管理基类
{
    protected override void 工序0()
    {
        人文_工序0();
    }

    private void 人文_工序0()
    {
        // ...
    }

    protected override void 工序2()
    {
        人文_工序2();
    }

    private void 人文_工序2()
    {
        // ...
    }
}

class 哲学书籍管理 : 书籍管理基类
{
    protected override void 工序0()
    {
        哲学_工序0();
    }

    private void 哲学_工序0()
    {
        // ...
    }

    protected override void 工序2()
    {
        哲学_工序2();
    }

    private void 哲学_工序2()
    {
        // ...
    }
}