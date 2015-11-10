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
using System.Runtime.Serialization;


namespace ScrimpNet.ServiceModel.BaseContracts
{

    /// <summary>
    /// Base class for message requests to service end points that require an authenticated session or identity
    /// </summary>
    [DataContract(Namespace=BaseUtils.ContractVersion)]
    public abstract class SecureSessionRequestBase:ServiceSessionRequestBase 
    {
        private string _sessionToken;
        public SecureSessionRequestBase()
        {

        }
        public SecureSessionRequestBase(string sessionToken)
        {
            _sessionToken = sessionToken;
        }

        public SecureSessionRequestBase(string sessionToken, string identityToken):this(sessionToken)
        {
            ActorToken = identityToken;
        }
        /// <summary>
        /// The identity of user that is going to perform the action of the request. 
        /// Identity tokens are returned from UserValidate() operation
        /// </summary>
        [DataMember]
        public string ActorToken { get; set; }
    }

    /// <summary>
    /// Base class for message responses from a secured method (one that requires a user identity for execution)
    /// </summary>
    [DataContract]
    public abstract class SecureSessionReplyBase : ServiceSessionReplyBase
    {
        public SecureSessionReplyBase() { }

        public SecureSessionReplyBase(SecureSessionRequestBase request)
            : base((ServiceSessionRequestBase)request)
        {

        }
        public SecureSessionReplyBase(Exception ex) : base(ex) { }
    }
}
