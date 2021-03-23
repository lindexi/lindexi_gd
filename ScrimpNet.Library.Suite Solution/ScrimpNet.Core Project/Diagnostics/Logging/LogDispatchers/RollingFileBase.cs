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
using System.Threading;
using System.IO;
using ScrimpNet.Text;

namespace ScrimpNet.Diagnostics
{
    /// <summary>
    /// Class that contains rolling file logic.
    /// </summary>
    public abstract class RollingLogBase : ILogDispatcher, IDisposable
    {

        /// <summary>
        /// Approximate size of log file before automatically creating a new file. Default: 50MB
        /// </summary>
        private int _maxFileSizeMb = CoreConfig.Log.MaximumFileSizeMb;
      
        /// <summary>
        /// Pre-calculate this value since it doesn't change
        /// </summary>
        private long _maxFileSizeBytes = ((long)CoreConfig.Log.MaximumFileSizeMb) * 1024L * 1024L;

        /// <summary>
        /// Location where file(s) will be written. Default ''
        /// </summary>
        private string _logFolder = CoreConfig.Log.LogFolder;

        /// <summary>
        /// Prefix to be added before log file sequence  Default: {app}.LogFile.{ts}
        /// </summary>
        private string _filePrefix = CoreConfig.Log.LogFilePrefix;

        /// <summary>
        /// DateTime format of timestamp portion of file name. Default: yyyy-MM-dd (for one log file / day)
        /// </summary>
        private string _timeStampFormat = CoreConfig.Log.TimeStampFormat;

        /// <summary>
        /// Extension to be added at the end of the file (csv, log, ...) Default: .log without leading '.'
        /// </summary>
        private string _fileSuffix = CoreConfig.Log.LogFileExtension;

        /// <summary>
        /// Default constructor
        /// </summary>
        public RollingLogBase()
        {
        }
        /// <summary>
        /// Path to folder containing log files written by this appender.  Default: ''
        /// </summary>
        public string logFolder
        {
            get
            {
                if (_logFolder.Contains("{ts}") == false) return _logFolder;
                string folder = _logFolder.Replace("{ts}", "{0:" + timeStampFormat + "}");
                return string.Format(folder, DateTime.Now);
            }
            set { _logFolder = value; }
        }

        /// <summary>
        /// Prefix of file name.  Appender will automatically append timestamp and .log extension to prefix. Default: Logfile.
        /// </summary>
        public string filePrefix
        {
            get
            {
                if (_filePrefix.Contains("{ts}") == false) return _filePrefix;
                string prefix = _filePrefix.Replace("{ts}", "{0:" + timeStampFormat + "}");
                return string.Format(prefix, DateTime.Now);
            }
            set { _filePrefix = value; }
        }

        /// <summary>
        /// Extension to be added at the end of the file (csv, log, ...) with {ts} expanded into datetime. Default: .log without leading '.'
        /// </summary>
        public string fileExtension
        {
            get
            {
                if (_fileSuffix.Contains("{ts}") == false) return _fileSuffix;
                string extension = _fileSuffix.Replace("{ts}", "{0:" + timeStampFormat + "}");
                return string.Format(extension, DateTime.Now);
            }
            set { _fileSuffix = value; }
        }



        /// <summary>
        /// format string to supply for DateTime.  Default is daily log rolling of: yyyy-MM-dd.  'Null' and empty strings '' are not permitted
        /// </summary>
        public string timeStampFormat
        {
            get { return _timeStampFormat; }
            set { _timeStampFormat = value; }
        }

        static RollingLogBase()
        {
            // initalizeCounters();

        }

        //-----------------------------------------------------------
        //  working member variables with default values set
        //-----------------------------------------------------------


        /// <summary>
        /// Approximate size of log file before automatically creating a new file. Default: 50MB
        /// </summary>
        public int maxFileSizeMb
        {
            get { return _maxFileSizeMb; }
            set { _maxFileSizeMb = value; }
        }


        /// <summary>
        /// fully expanded file name but without sequence numbers
        /// </summary>
        private string filenameKey
        {
            get
            {
                return Path.Combine(this.logFolder, filePrefix + "." + fileExtension);
            }
        }

        //-----------------------------------------------------------
        // caching locks
        //-----------------------------------------------------------
        private static ReaderWriterLockSlim _fileLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);  //locks operating system file being written to

        /// <summary>
        /// Build a fully qualified path+file for where logfile will be written to.  Handles all logic for file name rolling. Default: [current dir]\Logfile.
        /// </summary>
        /// <remarks>
        /// Filename is [prefix][DateStamp].[_sequence_.][suffix]
        /// </remarks>
        private string findLogFileName(int bytesToAddToFile)
        {
            string fileKey = filenameKey; //for performance copy properties to local variables
            string fileName = filePrefix;
            string fileExt = fileExtension;
            string logFldr = this.logFolder;

            //return Path.Combine(this.logFolder, "logfile.log");
            if (maxFileSizeMb <= 0) // no size rolling so add suffix and return name
            {
                return fileKey;
            }

            int sequenceNumber = 0;
            if (_activeSequence.ContainsKey(fileKey) == false) //first time through so find next file name
            {
                _activeSequence.Add(fileKey, 0);

                string searchPattern = TextUtils.StringFormat(fileName + ".*." + fileExt); //get all files for this fileName+extension
                string[] fileNameList = Directory.GetFiles(logFldr, searchPattern);
                List<FileInfo> fileList = new List<FileInfo>();

                foreach (string logFile in fileNameList)
                {
                    fileList.Add(new FileInfo(logFile));
                }

                if (fileList.Count > 0)
                {
                    fileList.Sort((left, right) => //sort list name which will result in highest sequence number sorting to end
                                      {
                                          return string.Compare(left.Name, right.Name);
                                      });
                    sequenceNumber = fileList.Count - 1; //'0' based file name index
                    _activeSequence[fileKey] = sequenceNumber;
                    _fileSizes[fileList[sequenceNumber].FullName] = fileList[sequenceNumber].Length;
                }
                else
                {

                    _activeSequence[fileKey] = sequenceNumber;
                }

                
            }

            if (_activeSequence.ContainsKey(fileKey) == false)
            {
                _activeSequence.Add(fileKey, 0);
            }
            else
            {
                sequenceNumber = _activeSequence[fileKey];
            }

            string currentPath = Path.Combine(logFldr, fileName + string.Format(".{0:0000}", sequenceNumber) + "." + fileExt);

            if (_fileSizes.ContainsKey(currentPath)==false)
            {
                _fileSizes.Add(currentPath, 0L);
            }

            long fileSize = _fileSizes[currentPath];
            sequenceNumber = _activeSequence[fileKey];

            if ((fileSize+bytesToAddToFile) > _maxFileSizeBytes ) //create new file name if adding this many bytes will exceed max limit
            {
                sequenceNumber++;
                _activeSequence[fileKey] = sequenceNumber;
                currentPath = Path.Combine(logFldr, fileName + string.Format(".{0:0000}", sequenceNumber) + "." + fileExt);
                _fileSizes[currentPath] = 0L;
                fileSize = 0;
            }

            fileSize = fileSize + bytesToAddToFile;
            _fileSizes[currentPath] = fileSize;

            return currentPath;

        }

        private Dictionary<string, int> _activeSequence = new Dictionary<string, int>();

        private Dictionary<string, long> _fileSizes = new Dictionary<string, long>();

        /// <summary>
        /// Value which is returned from here will be written to log file
        /// </summary>
        /// <param name="msg">Message that the formatter will add to the in-process string builder</param>
        /// <param name="sb">Builder that is collecting a group of messages and will be persisted once those messages are added to this builder</param>
        protected abstract void FormatMessage(StringBuilder sb, LogMessage msg);
        

        #region IDisposable Members

        /// <summary>
        /// Persist any remaining log entries in queue
        /// </summary>
        /// <param name="isDisposing">True is release both managed and unmanaged resources.  False for unmanaged only</param>
        protected void Dispose(bool isDisposing)
        {
            if (isDisposing == true)
            {
                //OnClose(); //flush pending records
                //release managed resources 
            }

            // Free your own state (unmanaged objects).
            // Set large fields to null.

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Class destructor.  Do not persist any queued log records
        /// </summary>
        ~RollingLogBase()
        {
            Dispose(false);
        }

        #endregion




        #region ILogPersister Members

        /// <summary>
        /// No special initialization needed
        /// </summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// Called when records need flushed to presistent store.  Must override in child class.  Default version doesn't do anything.
        /// </summary>
        /// <returns></returns>
        public virtual bool Flush()
        {

            return true;
        }

        /// <summary>
        /// Close log persister. Should override in child class.  Default version doesn't do anything
        /// </summary>
        /// <returns></returns>
        public virtual bool Close()
        {

            return true;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Close all allocated resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion



        /// <summary>
        /// Presist a group of messages to file storage
        /// </summary>
        /// <param name="messageBlock">Set of messages to be persisted to storage</param>
        /// <returns>True if all messages were persisted to storage medium</returns>
        public bool PersistMessages(IEnumerable<LogMessage> messageBlock)
        {
            List<LogMessage> messageList = messageBlock as List<LogMessage>;
            if (messageBlock == null) return false;
            try
            {

                try
                {
                    int listCount = messageList.Count;
                    StringBuilder sb = new StringBuilder(); //build a string containing all messages in messageBlock
                    for (int x = 0; x < listCount; x++)
                    {
                        LogMessage msg = messageList[x];

                        if (msg != null)
                        {
                            FormatMessage(sb,msg);
                        }
                    }
                    _fileLock.EnterWriteLock(); //block here and wait until log file is available
                    File.AppendAllText(findLogFileName(sb.Length), sb.ToString());
                    return true;
                }
                catch (Exception ex) //never throw an error on last chance log
                {
                    Log.LastChanceLog(ex);
                    return false;
                }
                finally
                {
                    if (_fileLock.IsWriteLockHeld == true)
                    {
                        _fileLock.ExitWriteLock();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LastChanceLog(ex); //swallow any exception
                return false;
            }
        }
    }
}
