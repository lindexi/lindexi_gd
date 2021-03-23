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
using ScrimpNet.Collections;
using System.Threading;

namespace ScrimpNet.Diagnostics
{
    public partial class Log
    {
        /// <summary>
        /// Async buffer of log messages
        /// </summary>
        private class MessageBuffer : AsyncBuffer<LogMessage>,IDisposable 
        {
            private static volatile bool _logInitialized = false;
            private static ReaderWriterLockSlim _initializeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            public static ReaderWriterLockSlim _dispatcherLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            private static volatile ILogDispatcher _logPersister;

            public override void OnClose()
            {
                base.OnClose();
                if (_logInitialized == true)
                {
                    _logPersister.Close();
                }
            }
            /// <summary>
            /// Called by base class when it wants to dump a page of objects out of the buffer
            /// </summary>
            /// <param name="pageOfObjects"></param>
            protected override void OnBufferAction(IEnumerable<LogMessage> pageOfObjects)
            {
                try
                {
                    _initializeLock.EnterUpgradeableReadLock();
                    if (_logInitialized == false)
                    {
                        try
                        {
                            _initializeLock.EnterWriteLock();
                            if (_logInitialized == false)
                            {
                                _logPersister = CoreConfig.Log.ActiveDispatcher;
                                _logPersister.Initialize();
                                _logInitialized = true;
                            }
                        }
                        finally
                        {
                            _initializeLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    _initializeLock.ExitUpgradeableReadLock();
                }

                try
                {
                    _dispatcherLock.EnterWriteLock();
                    _logPersister.PersistMessages(pageOfObjects);
                }
                catch
                {
                    _dispatcherLock.ExitWriteLock();
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                if (isDisposing == true)
                {
                    base.Dispose(isDisposing);
                    if (_logPersister != null)
                    {
                        _logPersister.Dispose();
                    }
                }
                GC.SuppressFinalize(this);
            }
            public override void Dispose()
            {
                Dispose(true);
            }
            ~MessageBuffer()
            {
                Dispose(false);
            }
        }
    }
}
