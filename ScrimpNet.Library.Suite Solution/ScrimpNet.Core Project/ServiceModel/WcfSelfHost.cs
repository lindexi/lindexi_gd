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
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
namespace ScrimpNet.ServiceModel
{
    /// <summary>
    /// Simple WCF self-hosting class for use in unit testing and short console programs.  Does not require configuration and does not require proxy class
    /// </summary>
    /// <typeparam name="T">type of the service implmentation</typeparam>
    /// <typeparam name="I">interface of service implementation</typeparam>
    public class WcfSelfHost<T, I> : IDisposable
    {
        /// <summary>
        /// {protocol}//{host}:{port}
        /// </summary>
        private const string _ENDPOINTURL = "{protocol}//{host}:{port}";
        ReaderWriterLockSlim _portLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private const ushort _BASEPORT = 44321;
        /// <summary>
        /// string == address
        /// </summary>
        private static Dictionary<string, ushort> _portMap = new Dictionary<string, ushort>();
        ServiceEndpoint _ep;
        ServiceHost _host = null;

        /// <summary>
        /// Default constructor.  Sufficient in most cases.  Binds to net.tcp protocol, local host, port &gt;= 44321 depending on number of instances invoked
        /// </summary>
        public WcfSelfHost()
        {
            EndpointAddress ea = buildEndPoint();

            _host = new ServiceHost(typeof(T), new Uri[] { ea.Uri });
            _ep = _host.AddServiceEndpoint(typeof(I), new NetTcpBinding(), ea.Uri);
            CommunicationState state = _host.State;
            _host.Open();
        }


        private EndpointAddress buildEndPoint()
        {
            string subAddress = typeof(T).Name;
            string addressKey = typeof(T).FullName;
            ushort hostPort = 0;
            try
            {
                _portLock.EnterUpgradeableReadLock();

                if (_portMap.ContainsKey(addressKey) == true)
                {
                    hostPort = (ushort)((ushort)_portMap[addressKey] + (ushort)_BASEPORT);
                }
                else
                {
                    try
                    {
                        _portLock.EnterWriteLock();
                        _portMap[addressKey] = (ushort)(_BASEPORT + _portMap.Count);
                        hostPort = _portMap[addressKey];
                    }
                    finally
                    {
                        _portLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _portLock.ExitUpgradeableReadLock();
            }

            string endPoint = _ENDPOINTURL;
            string protocol = "net.tcp:";
            string host = "localhost/"+subAddress;
            string port = hostPort.ToString();

            endPoint = endPoint.Replace("{protocol}", protocol).Replace("{host}", host).Replace("{port}", port);
            return new EndpointAddress(endPoint);

        }

        #region IDisposable Members

        /// <summary>
        /// Close serivce host
        /// </summary>
        /// <param name="isDisposing">True if called from Dispose</param>
        public void Dispose(bool isDisposing)
        {
            if (isDisposing == true)
            {
                if (_host != null)
                {
                    try
                    {
                        switch (_host.State)
                        {
                            case CommunicationState.Created:
                                break;
                            case CommunicationState.Opening:
                                _host.Close(new TimeSpan(0, 0, 0, 0, 500));
                                break;
                            case CommunicationState.Opened:
                                _host.Close(new TimeSpan(0,0,0,0,500));
                                break;
                            case CommunicationState.Faulted:
                                break;

                        }
                    }
                    catch //just in case something went wrong
                    {
                        _host.Abort();
                    }

                    _host = null;
                }
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implmentation of IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~WcfSelfHost()
        {
            Dispose(false);
        }
        private I _client;
        private bool _isClientSet = false;

        /// <summary>
        /// Get a client reference to the service (called a 'Channel' in WCF) so you can send messages to service
        /// </summary>
        public I Client
        {
            get
            {
                if (_isClientSet == false)
                {                    
                    ChannelFactory<I> factory = new ChannelFactory<I>(_ep.Binding, _ep.Address);
                    _client = factory.CreateChannel();
                    _isClientSet = true;                    
                }
                
                return _client;
            }
        }
        
        #endregion
    }
}
