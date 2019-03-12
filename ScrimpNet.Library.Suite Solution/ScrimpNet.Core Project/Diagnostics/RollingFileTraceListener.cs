using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using ScrimpNet.Collections;
using System.Collections.Specialized;

namespace ScrimpNet.Diagnostics
{
    public class RollingFileTraceListener : TraceListener
    {
        
        public RollingFileTraceListener():base()
        {

        }

        public RollingFileTraceListener(string name)
            : base(name)
        {

        }

        public string folderPath { get; set; }
        private string _initData;
        public string initData
        {
            get { return _initData; }
            set { _initData = value; }
        }
        protected override string[] GetSupportedAttributes()
        {
            return new string[] { "folderPath" };
        }
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
           // base.TraceData(eventCache, source, eventType, id, data);
        }
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {

           // base.TraceData(eventCache, source, eventType, id, data);
        }
        public override void Write(string message)
        {
           
        }
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            base.TraceEvent(eventCache, source, eventType, id);
        }
        public override void WriteLine(string message)
        {
            StringDictionary attrs = base.Attributes;
        }

        public override void Fail(string message)
        {
            base.Fail(message);
        }
        public override void Fail(string message, string detailMessage)
        {
            base.Fail(message, detailMessage);
        }

        public override void Write(object o)
        {
            base.Write(o);
        }
        public override void Write(object o, string category)
        {
            base.Write(o, category);
        }
        public override void Write(string message, string category)
        {
            base.Write(message, category);
        }
        public override void WriteLine(object o)
        {
            base.WriteLine(o);
        }
        public override void  WriteLine(object o, string category)
{
 	 base.WriteLine(o, category);
}

        public override void WriteLine(string message, string category)
        {
            base.WriteLine(message, category);
        }
        public override void Flush()
        {
            base.Flush();
        }


    }
}
