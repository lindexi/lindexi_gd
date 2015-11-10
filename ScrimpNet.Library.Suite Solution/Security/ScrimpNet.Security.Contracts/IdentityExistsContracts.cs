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
using ScrimpNet.ServiceModel.BaseContracts;

namespace ScrimpNet.Security.Contracts
{
    [DataContract(Namespace=ContractConfig.ContractVersion)]
    public class IdentityExistsRequest: ServiceSessionRequestBase
    {
        public IdentityExistsRequest(){}

        public IdentityExistsRequest(string serviceSessionToken, string identityName)
        {
            base.ServiceSessionToken = serviceSessionToken;
            IdentityName = identityName;
        }

        [DataMember]
        public string IdentityName { get; set; }
    }

    [DataContract(Namespace=ContractConfig.ContractVersion)]
    public class IdentityExistsReply : ServiceSessionReplyBase
    {
        public IdentityExistsReply() { }
        public IdentityExistsReply(ServiceSessionRequestBase request)
            : base(request)
        {
            IdentityExists = false;
        }

        [DataMember]
        public bool IdentityExists { get; set; }
    }
}
