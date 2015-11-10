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
using System.Security.Principal;
using System.Runtime.Serialization;
using ScrimpNet.ServiceModel.BaseContracts;

namespace ScrimpNet.Security.Contracts
{
    /// <summary>
    /// Verify credentials presented to security system
    /// </summary>
    [DataContract(Namespace=ContractConfig.ContractVersion)]
    public class UserVerifyRequest : ServiceSessionRequestBase 
    {
        
        /// <summary>
        /// Often called 'username' or 'login name'
        /// </summary>
        [DataMember]
        public string ChallengePrompt { get; set; }

        /// <summary>
        /// Often called 'password'
        /// </summary>
        [DataMember]
        public string ChallengeAnswer { get; set; }
    }

    /// <summary>
    /// Verify credentials and hydrate security context if successful
    /// </summary>
    [DataContract(Namespace=ContractConfig.ContractVersion)]
    public class UserVerifyReply : SecureSessionReplyBase 
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public UserVerifyReply() 
        {

        }

        private SecurityContext _context;
        /// <summary>
        /// Context set if UserVerify operation was successful.  Implements IIdentity, IPrincipal
        /// </summary>
        [DataMember]
        public SecurityContext Context
        {
            get
            {
                if (_context == null)
                {
                    _context = new SecurityContext();
                }
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        /// <summary>
        /// Copy transfer fields from request
        /// </summary>
        /// <param name="request">Request to copy fields from</param>
       public UserVerifyReply(UserVerifyRequest request):base()
        {
            this.RequestorData = request.RequestorData;            
        }

        /// <summary>
        /// Facade over Context.IsAuthenticated.  Note: Setting value to False will erase current security context
        /// </summary>
        [DataMember]
        public bool IsAuthenticated
        {
            get
            {
                if (Context.IsAuthenticated == false) return false;
                return true;
            }
            set
            {
                Context.IsAuthenticated = value;
            }
        }
    }


}
