using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ScrimpNet.Diagnostics;
using System.Collections.Specialized;

namespace TraceListener_Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //Trace.Listeners.Clear();
            //Trace.Listeners.Add(new RollingFileTraceListener());
            //Trace.TraceError("hello");
            //Trace.Fail("fail messaage");
            //Trace.Fail("summary message", "detail message");
            //Trace.TraceError("Trace error");
            //Trace.TraceError("Trace with params {0}", DateTime.Now);
            //Trace.TraceInformation("Info");
            //Trace.TraceInformation("Info with params {0}", DateTime.Now);
            //Trace.TraceWarning("Warning");
            //Trace.TraceWarning("Waring with params {0}", DateTime.Now);

            System.Diagnostics.TraceSource ts = new TraceSource("mysource", SourceLevels.All);
        
            StringDictionary attrs = ts.Attributes;
            ts.TraceInformation("this is an info message");
            ts.TraceData(TraceEventType.Error, 300, "hello");


        }
    }
}
