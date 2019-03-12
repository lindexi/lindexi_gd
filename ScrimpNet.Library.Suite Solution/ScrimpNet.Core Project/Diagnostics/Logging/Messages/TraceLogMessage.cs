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
using System.Linq;
using System.Text;
using ScrimpNet.Diagnostics;

namespace ScrimpNet.Diagnostics
{

    /// <summary>
    /// Created by a Tracer when it disposes.
    /// </summary>

    public class TracerLogMessage : LogMessage
    {
        /// <summary>
        /// Time when tracer started
        /// </summary>
        public long EnterTicks { get; set; }

        /// <summary>
        /// Time when tracer stopped
        /// </summary>
        public long ExitTicks { get; set; }

        /// <summary>
        /// Number seconds a tracer took to execute
        /// </summary>
        public long ElapsedTimeMs { get; set; }

        /// <summary>
        /// Unique identifier of this particular trace
        /// </summary>
        public Guid TracerId { get; set; }
        /// <summary>
        /// Category, if any, this tracer represents.  (e.g Code, Database method, etc)
        /// </summary>
        public string TracerCategory { get; set; }

        /// <summary>
        /// Used for correlation studies.  Should be the trace that called this one.
        /// </summary>
        public Guid ParentOperationId { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TracerLogMessage()
            : base()
        {
            //this.LogMessageClass = LogMessageCategory.TraceLogging;
            this.Severity = MessageLevel.Trace;

        }

        /// <summary>
        /// Outputs properties of this class.  Generally used in logging scenarios
        /// </summary>
        /// <returns>Format of each trace</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.AppendFormat("      EnterTicks: {0}{1}", this.EnterTicks, Environment.NewLine);
            sb.AppendFormat("      Exit Ticks: {0}{1}", this.ExitTicks, Environment.NewLine);
            sb.AppendFormat("   ElapsedTimeMs: {0}{1}", this.ElapsedTimeMs, Environment.NewLine);
            sb.AppendFormat("  Probe Category: {0}{1}", this.TracerCategory, Environment.NewLine);
            sb.AppendFormat("Parent Operation: {0}{1}", this.ParentOperationId, Environment.NewLine);
			
            return sb.ToString();

        }
    }
}
