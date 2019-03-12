/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace ScrimpNet.Diagnostics
{

    /// <summary>
    /// A sentinel class that can be used in place of explicit method entrance and exit traces.  Typically a TraceProbe is instantiated in a using() statement that wraps an
    /// entire method.  The tracer can automatically log it's exit upon going out of scope
    /// </summary>
    public class TraceProbe : IDisposable
    {
        //private const int CALLER_FRAME = 1;
		private class LabelItem
		{
			public DateTime LabelTime { get; set; }
			public string Message { get; set; }
			public TimeSpan Elapsed { get; set; }
			public TimeSpan Interval { get; set; }
		}
		
        private bool _enabled;
        private Log _log;
        private Stopwatch _stopwatch;
        private long _enterTicks = 0;
        private long _exitTicks = 0;
        private string _category = "Code";
        private Guid _tracerId = Guid.NewGuid();
        private Guid _parentTrace = Guid.Empty;
		private List<LabelItem> _traceLabels = new List<LabelItem>();
        /// <summary>
        /// Default constructor
        /// </summary>
        internal TraceProbe()
        {
			_priorMark = DateTime.Now;
        }

        /// <summary>
        /// Create a tracer that is bound to a specific category.  Categories are used for tracer analysis
        /// </summary>
        /// <param name="log">Name of log that is performing this trace</param>
        /// <param name="tracerCategory">String that will group different kinds of traces (e.g. Code, Database Call, WebService, etc).  Default: 'Code'</param>
        internal TraceProbe(Log log, string tracerCategory)
           
        {
            _category = string.IsNullOrEmpty(tracerCategory) ? "Code" : tracerCategory;
            _log = log;
            _enabled = _log.IsTraceEnabled;
            if (_enabled)
            {
                if (Trace.CorrelationManager.LogicalOperationStack.Count > 0)
                {
                    _parentTrace = Transform.ToGuid(Trace.CorrelationManager.LogicalOperationStack.Peek());
                }
                Trace.CorrelationManager.StartLogicalOperation(_tracerId);
                _enterTicks = DateTime.Now.Ticks;
                _exitTicks = 0;
                sendLogMessage();
                _stopwatch = new Stopwatch();
				_priorMark = DateTime.Now;
                _stopwatch.Start();
            }
        }

        /// <summary>
        /// Constructor that associates an active log with a particular trace.  Default TraceProbe category is 'Code'
        /// </summary>
        /// <param name="log"></param>
        public TraceProbe(Log log):this(log,"Code")
        {
        }

        /// <summary>
        /// Constructor that stored the caller's frame which represents the source of the trace
        /// </summary>
        /// <param name="log">Log to assoicate with this trace</param>
        /// <param name="targetFrame">Call stack location of caller</param>
        /// <param name="traceCategory">String that will group different kinds of traces (e.g. Code, Datbase Call, WebService, etc)</param>
        internal TraceProbe(Log log, StackFrame targetFrame, string traceCategory)
        {
            _log = log;
            _enabled = _log.IsTraceEnabled;

            if (_enabled)
            {
                if (Trace.CorrelationManager.LogicalOperationStack.Count > 0)
                {
                    _parentTrace = Transform.ToGuid(Trace.CorrelationManager.LogicalOperationStack.Peek());
                }
                Trace.CorrelationManager.StartLogicalOperation(_tracerId);
                _enterTicks = DateTime.Now.Ticks;
                _exitTicks = 0;
                _category = string.IsNullOrEmpty(traceCategory) ? "Code" : traceCategory;
                _stopwatch = new Stopwatch();
                sendLogMessage();
				_priorMark = DateTime.Now;
                _stopwatch.Start();
            }
        }

		/// <summary>
		/// Creates a label in this trace including time/date information.  Used for monitoring inside a single trace.  Captures elapsed time from
		/// start of trace and interval time from prior trace point
		/// </summary>
		/// <param name="message">Any text to identify with this label.  May include any .Net format specifiers</param>
		/// <param name="args">Value to supply to message format specifiers</param>
		public void TracePoint(string message, params object[] args)
		{
			_stopwatch.Stop();
			try
			{
				_traceLabels.Add(new LabelItem()
					{
						LabelTime = DateTime.Now,
						Message = string.Format(message, args),
						Elapsed = _stopwatch.Elapsed,
						Interval = DateTime.Now - _priorMark
					});
			}
			finally
			{
				_priorMark = DateTime.Now;
				_stopwatch.Start();
			}
		}

		DateTime _priorMark;
		/// <summary>
		/// Creates a mark in this trace including time/date information.  Used for monitoring inside a single trace.  Captures elapsed time from
		/// start of trace and interval time from prior trace point
		/// </summary>
		public void TracePoint()
		{
			TracePoint("");
		}
        private void sendLogMessage()
        {
            TracerLogMessage _logMessage = new TracerLogMessage();
            _logMessage.TracerCategory = this.Category;
            _logMessage.ElapsedTimeMs = 0;
            if (_exitTicks != 0L)
            {
                _logMessage.ElapsedTimeMs = (long)(new TimeSpan(_exitTicks - _enterTicks).TotalMilliseconds);
            }
			for(int x=0;x<_traceLabels.Count;x++)
			{				
				_logMessage.ExtendedProperties.Add("TracePoint:"+x.ToString("00"),string.Format("@{0:hh:mm:ss} (+{1:0.000} interval {2:0.000} seconds,) {2}",_traceLabels[x].LabelTime,_traceLabels[x].Elapsed.TotalSeconds,_traceLabels[x].Interval,_traceLabels[x].Message).Trim());
			}
            _logMessage.EnterTicks = _enterTicks;
            _logMessage.ExitTicks = _exitTicks;
            _logMessage.TracerId = _tracerId;
            _logMessage.ParentOperationId = _parentTrace;
            _log.Write(_logMessage);
        }

        /// <summary>
        /// If tracing is enabled
        /// </summary>
        public bool IsEnabled
        {
            get { return _enabled; }
        }

        /// <summary>
        /// If stopwatch is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                if (_enabled == true)
                {
                    return _stopwatch.IsRunning;
                }
                return false;
            }
        }
        /// <summary>
        /// String that can group several traces into a particular type.  E.g. 'Code', 'Db','Feed', 'InternalWs'
        /// </summary>
        public string Category
        {
            get { return _category; }
            set { _category = value; }
        }
        #region IDisposable Members

        /// <summary>
        /// Stop timing and write log entries to file
        /// </summary>
        /// <param name="isDisposing">true if being called from Disposable, false if from Finalize</param>
        public void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_enabled)
                {
                    _exitTicks = DateTime.Now.Ticks;
					_stopwatch.Stop();
                    if (Trace.CorrelationManager.LogicalOperationStack.Count > 0)
                    {
						Trace.CorrelationManager.StopLogicalOperation();
                    }                    
                    sendLogMessage();
                }
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of things
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~TraceProbe()
        {
            Dispose(false);
        }
        #endregion
    }
}
