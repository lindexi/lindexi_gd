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

namespace ScrimpNet
{
    /// <summary>
    /// How external source material incorporated into library
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum CitationType
    {
        /// <summary>
        /// No specified or doesn't matter
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Source led to developing original work
        /// </summary>
        InspiredBy = 1,

        /// <summary>
        /// Referred to work by another for understanding or explaination.
        /// </summary>
        Referenced = 2,

        /// <summary>
        /// Source of additional, collary study or information
        /// </summary>
        CrossReference = 4,

        /// <summary>
        /// Took source ideas and reworked and enhanced them
        /// </summary>
        IdeaClone = 4,

        /// <summary>
        /// 5% of attributed object is from citied source
        /// </summary>
        Percent005Source = 8,

        /// <summary>
        /// 10% of attributed object is from citied source
        /// </summary>
        Percent010Source = 16,

        /// <summary>
        /// 25% of attributed object is from citied source
        /// </summary>
        Percent025Source = 32,

        /// <summary>
        /// 50% of attributed object is from citied source
        /// </summary>
        Percent050Source= 64,

        /// <summary>
        /// 75% of attributed object is from citied source
        /// </summary>
        Percent075Source=128,

        /// <summary>
        /// 100% of attributed object is from citied source
        /// </summary>
        Percent100Source = 256,

        /// <summary>
        /// Some of the attributed object is from cited source
        /// </summary>
        SomeSource,

        /// <summary>
        /// Much of the attributed object is from cited source
        /// </summary>
        MuchSource,

        ///<summary>
        /// All of the attributed object is from cited source 
        /// </summary>
        AllSource,
    }

    /// <summary>
    /// Used to give citations to code taken in part or in whole from another source.  A citation does not imply the entire
    /// object is from a different source. A citation could mean (Explict copy, copy with some mods, idea copied, inspired by).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments"), AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public sealed class CitationAttribute:System.Attribute
    {
        private string _citationId;

        /// <summary>
        /// Unqiue identifer in citation index
        /// </summary>
        public string CitationId
        {
            get { return _citationId; }
            set { _citationId = value; }
        }

        private string _author;

        /// <summary>
        /// Author(s) of source material
        /// </summary>
        public string Author
        {
            get { return _author; }
            set { _author = value; }
        }

        private string _sourceDocument;

        /// <summary>
        /// Source (usually URL) citation
        /// </summary>
        public string Source
        {
            get { return _sourceDocument; }
            set { _sourceDocument = value; }
        }

        private string _sourceDate;

        /// <summary>
        /// Date of source material
        /// </summary>
        public string SourceDate
        {
            get { return _sourceDate; }
            set { _sourceDate = value; }
        }

        private string _downloadDateTime;

        /// <summary>
        /// Date/Time material used or gathered
        /// </summary>
        public string AcquiredDate
        {
            get { return _downloadDateTime; }
            set { _downloadDateTime = value; }
        }

        private CitationType _citationType;

        /// <summary>
        /// Determines what kind of license source has associated with it (if known)
        /// </summary>
        public CitationType CitationType
        {
            get { return _citationType; }
            set { _citationType = value; }
        }

        private string _license;

        /// <summary>
        /// Short license code (GPL, OpenSource, etc) the cited work is exposed under
        /// </summary>
        public string License
        {
            get { return _license; }
            set { _license = value; }
        }

        private string _libraryId;

        /// <summary>
        /// Key to citation library
        /// </summary>
        public string LibraryId
        {
            get { return _libraryId; }
            set { _libraryId = value; }
        }

        private string _notes;

        /// <summary>
        /// Miscellaneous textual information citer may want to add to citation
        /// </summary>
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; }
        }
	
	    /// <summary>
	    /// Citation constructor
	    /// </summary>
	    /// <param name="citationId">Unique identifer for this citation.  Often datetime stamp variant</param>
        public CitationAttribute(string citationId)
        {
            _citationId = citationId;
        }
    }
}
