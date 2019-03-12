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
using System.IO;
using System.Threading;
using ScrimpNet.Configuration;
using ScrimpNet.Diagnostics;

namespace ScrimpNet.Collections
{
    /// <summary>
    /// Action to take when buffer determines it is time to flush it's items
    /// </summary>
    /// <typeparam name="T">Type of object that is being buffered</typeparam>
    /// <param name="bufferedItems">List of items that will be acted on by the buffers Flush action</param>
    public delegate void AsyncBufferAction<T>(IEnumerable<T> bufferedItems);
  
    /// <summary>
    /// This class is responsible for collecting (buffering) objects before spooling them off to the
    /// action supplied in the constructor.  This is the first level of caching.  The action might also implement
    /// it's own caching and asynchronous behavior (e.g ScrimpNet rolling log file writers).  
    /// This is the second level of caching.
    /// </summary>
    /// <typeparam name="T">Type of object that is being buffered</typeparam>
    public abstract class AsyncBuffer<T> : IDisposable
    {

        //-----------------------------------------------------------
        //  Default values if not provided from .config file
        //-----------------------------------------------------------
        /// <summary>
        /// 300
        /// </summary>
        private const int MAX_PAGE_SIZE = 300;

        /// <summary>
        /// 1000 MS
        /// </summary>
        private const int BUFFER_SWEEP_MS = 1000;

        /// <summary>
        /// 1000 MS
        /// </summary>
        private const int CACHE_TIMEOUT_MS = 1000;

        //-----------------------------------------------------------
        //  working member variables with default values set
        //-----------------------------------------------------------

        /// <summary>
        /// Number of objects (called events in log4Net or LogEntry in Enterprise Library) which will be sent to the buffer's action
        /// Default: 300 Records
        /// </summary>
        private int _maxPageSize = MAX_PAGE_SIZE;

        /// <summary>
        /// Number of milliseconds appender will wait before executing any accumulated records (for partial pages)
        /// Default: 1000 Ms.  Config Settings: ScrimpNet.Buffer.Sweep
        /// </summary>
        private int _bufferSweepMs = BUFFER_SWEEP_MS;

        /// <summary>
        /// Number of milliseconds the cache may lay dormant (without new records being added) until the buffer is automatically flushed
        /// Default: 1000 ms.  Config Settings: ScrimpNet.Buffer.CacheTimeOutMs
        /// </summary>
        private int _cacheTimeOutMs = CACHE_TIMEOUT_MS;

         /// <summary>
        /// Number of objects (called events in log4Net) which will be acted on 
        /// Default: 300 Records.  Config Settings: ScrimpNet.Buffer.MaxPageSize
        /// </summary>
        public int MaxPageSize
        {
            get { return _maxPageSize; }
            set { _maxPageSize = value; }
        }

        
        /// <summary>
        /// Number of milliseconds appender sleep between writting full buffers to storage
        /// Default: 1000 Ms. Config Settings: ScrimpNet.Buffer.Sweep
        /// </summary>
        public int BufferSweepMs
        {
            get { return _bufferSweepMs; }
            set { _bufferSweepMs = value; }
        }

        /// <summary>
        /// Number of milliseconds the cache may lay dormant (without new records being added) until the buffer is automatically flushed
        /// Default 1000 ms, Config Settings: ScrimpNet.Buffer.CacheTimeOutMs
        /// </summary>
        public int CacheTimeOutMs
        {
            get { return _cacheTimeOutMs; }
            set { _cacheTimeOutMs = value; }
        }

        private AsyncBufferAction<T> _bufferAction;

        //-----------------------------------------------------------
        // caching locks
        //-----------------------------------------------------------
        private  ReaderWriterLockSlim _pageLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion); //locks currentPage of objects
        private  ReaderWriterLockSlim _actionLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);  //locks operating system file being written to
        private  ReaderWriterLockSlim _queueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion); //locks buffer of queued up pages of objects messages

        //-----------------------------------------------------------
        // queued objects and list of pages
        //-----------------------------------------------------------
        private  List<T> _currentPage = new List<T>(); //buffer to accumulate messages from object subsystem
        
        private  Queue<List<T>> _eventQueue = new Queue<List<T>>(10); //preallocate MaxPageSize*10. (3000 objects/second for default).  

        private  DateTime _lastMessageReceived = DateTime.Now.AddDays(-365); //used for calculating cache timeouts
        
        private Timer _timer = null; //Used for sweeping the queue of messages
      

        /// <summary>
        /// Default constructor.  Start buffer sweep timer which will look through log queue and persist all pages of log messages is finds
        /// </summary>
        public AsyncBuffer()
        {
            _bufferSweepMs = ConfigManager.AppSetting<int>("Logging.Default.BufferSweepMs", BUFFER_SWEEP_MS);
            _maxPageSize = ConfigManager.AppSetting<int>("Logging.Default.MaxPageSize", MAX_PAGE_SIZE);
            _cacheTimeOutMs = ConfigManager.AppSetting<int>("Logging.Default.CacheTimeOutMs", CACHE_TIMEOUT_MS);

            _timer = new Timer(sweepQueue, null, _bufferSweepMs, _bufferSweepMs);
            _timer.Change(0, _bufferSweepMs);
            _bufferAction = OnBufferAction;
        }

        /// <summary>
        /// Event handler to execute when buffer needs to flush page(s) of objects
        /// </summary>
        /// <param name="objectItems"></param>
        protected abstract void OnBufferAction(IEnumerable<T> objectItems);

        /// <summary>
        /// Constructor that supplies an explict Action />
        /// </summary>
        /// <param name="bufferAction"></param>
        public AsyncBuffer(AsyncBufferAction<T> bufferAction):this()
        {
            _bufferAction = bufferAction;
        }

       
        /// <summary>
        /// Add an object to the list of queued up objects.  Only queuing is done 
        /// so the enqueing thread can return as quickly as possible.
        /// </summary>
        /// <param name="objectToBuffer">Entry that will be persisted</param>
        public void Submit(T objectToBuffer)
        {
            try
            {
                _pageLock.EnterUpgradeableReadLock();
                try
                {
                    _pageLock.EnterWriteLock();

                    _lastMessageReceived = DateTime.Now; //mark _currentPage as not stale

                    _currentPage.Add(objectToBuffer); //store event

                    if (_currentPage.Count > _maxPageSize) //submit page of objects (messages) to queue for persisting on next sweep
                    {
                        try
                        {
                            _queueLock.EnterWriteLock();
                            _eventQueue.Enqueue(_currentPage);
                        }
                        finally
                        {
                            _queueLock.ExitWriteLock();
                        }
                        _currentPage = new List<T>(_maxPageSize);
                    }
                }
                finally
                {
                    _pageLock.ExitWriteLock();
                }
            }
            finally
            {
                if (_pageLock.IsUpgradeableReadLockHeld == true)
                {
                    _pageLock.ExitUpgradeableReadLock();
                }
            }
        }

        /// <summary>
        /// Called when application is shutting down (not when a logger goes out of scope)
        /// </summary>
        public virtual void OnClose()
        {
            _timer.Change(0, Timeout.Infinite); //suspend timer
            //-------------------------------------------------------
            // put any partially filled buffer directly into persistent store
            //-------------------------------------------------------
            try
            {
                _pageLock.EnterWriteLock();
                _queueLock.EnterWriteLock();
                _actionLock.EnterWriteLock();
                while(_eventQueue.Count > 0)
                {
                    _bufferAction(_eventQueue.Dequeue());
                }
                if (_currentPage.Count >= 0) //there are records to be flushed
                {
                    _bufferAction(_currentPage); // will block until other threads relinquish the persistent store
                }

            }
            catch (Exception ex)
            {
                Log.LastChanceLog(ex, "Error when closing AsyncBuffer<{0}>",typeof(T).FullName);
            }
            finally
            {
                if (_actionLock.IsWriteLockHeld == true)
                {
                    _actionLock.ExitWriteLock();
                }
                if (_queueLock.IsWriteLockHeld)
                {
                    _queueLock.ExitWriteLock();
                }
                if (_pageLock.IsWriteLockHeld)
                {
                    _pageLock.ExitWriteLock();
                }
            }
        }



        /// <summary>
        /// Look over queue of object pages and create worker threads for each page.  Threads are responsible for
        /// persisting their page to persistent store
        /// </summary>
        /// <param name="context">not used.  Needed for compatibility with OnTimer event hander</param>
        private void sweepQueue(object context)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite); //suspend timer

            try
            {
                try
                {
                    if (_queueLock.TryEnterUpgradeableReadLock(1000) == false) return; //can't get lock after 1 second so ignore and try again on next sweep
                    if (_eventQueue.Count > 0)
                    {
                        List<List<T>> objectPages = new List<List<T>>();
                        try
                        {
                            if (_queueLock.TryEnterWriteLock(1000) == false) return; //can't get lock after 1 second so ignore
                            
                            while (_eventQueue.Count > 0)  //empty buffer of objects as quickly as possible
                            {
                                objectPages.Add(_eventQueue.Dequeue());
                            }
                        }
                        finally
                        {
                            _queueLock.ExitWriteLock();
                        }
                        // queue is now free to accept additional pages of inbound objects
                        // now take pages and flush them out of the buffer
                        try
                        {

                            _actionLock.EnterWriteLock();
                            foreach (List<T> objectPage in objectPages)
                            {
                                _bufferAction(objectPage);
                                //do events goes here
                            }
                        }
                        catch(Exception ex)
                        {
                            Log.LastChanceLog(ex, "Error trying to flush objects out of buffer.");
                        }
                        finally
                        {
                            if (_actionLock.IsWriteLockHeld == true)
                            {
                                _actionLock.ExitWriteLock();
                            }
                        }

                    }
                }
                catch (ThreadAbortException)
                {
                    //swallow thread abort exception thrown when system is shutting down
                }
                catch (Exception ex)
                {
                    Log.LastChanceLog(ex, "Error queuing pages to thread pool");
                    return;
                }
                finally
                {
                    if (_queueLock.IsUpgradeableReadLockHeld)
                    {
                        _queueLock.ExitUpgradeableReadLock();
                    }
                }
                //-------------------------------------------------------
                // if buffer is 'stale' then flush partial page
                //-------------------------------------------------------
                if (_pageLock.IsWriteLockHeld == false) //not currently appending records to buffer
                {
                    try
                    {
                        _pageLock.EnterUpgradeableReadLock();
                        if (_currentPage.Count > 0)
                        {
                            DateTime currentDate = DateTime.Now;

                            if (currentDate.Subtract(_lastMessageReceived).TotalMilliseconds > _cacheTimeOutMs)
                            {
                                try
                                {
                                    _pageLock.EnterWriteLock();
                                    if (currentDate.Subtract(_lastMessageReceived).TotalMilliseconds > _cacheTimeOutMs)
                                    {
                                        try
                                        {
                                            _actionLock.EnterWriteLock();
                                            _bufferAction(_currentPage);
                                        }
                                        finally
                                        {
                                            _actionLock.ExitWriteLock();
                                        }
                                        _currentPage = new List<T>(_maxPageSize);
                                    }
                                }
                                finally
                                {
                                    if (_pageLock.IsWriteLockHeld)
                                    {
                                        _pageLock.ExitWriteLock();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LastChanceLog("Error flushing partial buffer.{0}", Utils.Expand(ex));
                    }
                    finally
                    {
                        if (_pageLock.IsUpgradeableReadLockHeld)
                        {
                            _pageLock.ExitUpgradeableReadLock();
                        }
                    }
                } //if (_pageLock.IsWriteLockHeld == false) //not currently appending records to buffer
            }
            catch (Exception ex) //always swallow any unhandled exceptions.  Logging should never throw an error
            {
                Log.LastChanceLog(ex);
            }
            finally //always
            {
                _timer.Change(0, _bufferSweepMs); //restart timer
            }
        }

    //    protected abstract void OnFlushPage(IEnumerable<T> pageOfObjects);

        #region IDisposable Members

        /// <summary>
        /// Persist any remaining log entries in queue
        /// </summary>
        /// <param name="isDisposing">True is release both managed and unmanaged resources.  False for unmanaged only</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing == true)
            {
                OnClose(); //flush pending records
                //release managed resources 
            }

            // Free your own state (unmanaged objects).
            // Set large fields to null.

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose 
        /// </summary>
        public virtual void Dispose()
        {
            this.Dispose(true);
        }
        /// <summary>
        /// Class destructor.  Do not persist any queued log records
        /// </summary>
        ~AsyncBuffer()
        {
            Dispose(false);
        }

        #endregion
    }
}
