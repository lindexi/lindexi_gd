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
    [DataContract(Namespace=BaseUtils.ContractVersion)]
    public abstract class ServiceSessionRequestBase : RequestBase
    {
        public ServiceSessionRequestBase()
            : base()
        {
        }

        public ServiceSessionRequestBase(string serviceSessionToken)
        {
            ServiceSessionToken = serviceSessionToken;
        }

        /// <summary>
        /// Key that is used for this session.  The same session can be used with different identities (e.g. impersonation)
        /// </summary>
        [DataMember]
        public string ServiceSessionToken { get; set; }
    }

    /// <summary>
    /// Fields that are always returned as a response to calls to secured end-point methods
    /// </summary>
    [DataContract]
    public abstract class ServiceSessionReplyBase : ReplyBase
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public ServiceSessionReplyBase() { }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="request">Request containing values that will be transferred to response</param>
        public ServiceSessionReplyBase(ServiceSessionRequestBase request)
            : base((RequestBase)request)
        {
            ServiceSessionToken = request.ServiceSessionToken;
        }

        public ServiceSessionReplyBase(Exception ex) : base(ex) { }
        

        /// <summary>
        /// Key that has been assigned to this session.  NOTE: This key
        /// might change between calls so be sure to use this key in the
        /// next request
        /// </summary>
        [DataMember]
        public string ServiceSessionToken { get; set; }
    }

}
