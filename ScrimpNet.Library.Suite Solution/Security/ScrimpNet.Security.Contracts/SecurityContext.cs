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

namespace ScrimpNet.Security.Contracts
{
    /// <summary>
    /// Context set if UserVerify operation was successful.  Implements IIdentity, IPrincipal
    /// </summary>

    [DataContract(Namespace=ContractConfig.ContractVersion)]
    public class SecurityContext : IIdentity, IPrincipal
    {
        /// <summary>
        /// If authenticated will contain roles that are 
        /// currently assigned for this user.  A convenience method so
        /// a second call doesn't need to be made to service to retrieve roles
        /// </summary>
        [DataMember]
        public RoleListContract Roles { get; set; }

        /// <summary>
        /// Gets the type of authentication used. (IIdentity)
        /// </summary>
        [DataMember]
        public string AuthenticationType { get; set; }

        private bool _isAuthenticated = false;

        /// <summary>
        /// Gets a value that indicates whether the user has been authenticated. (IIdentity)
        /// </summary>
        [DataMember]
        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
            set { _isAuthenticated = value; }
        }

        /// <summary>
        /// Gets the name of the current user.
        /// </summary>
        [DataMember]
        public string Name { get; set; }


        /// <summary>
        /// The System.Security.Principal.IIdentity object associated with the current
        ///     principal.  (IPrincipal)
        /// </summary>
        [DataMember]
        public IIdentity Identity { get; set; }

        /// <summary>
        /// Determines whether the current principal belongs to the specified role. (IPrincipal)
        /// </summary>
        /// <param name="role">The name of the role for which to check membership</param>
        /// <returns>true if the current principal is a member of the specified role; otherwise,false.</returns>
        public bool IsInRole(string role)
        {
            return false;
        }

        /// <summary>
        /// 'Session' token that must be passed to any
        /// subsequent authenticated requests and identifies
        /// the authenticated user.
        /// </summary>
        [DataMember]
        public string IdentityToken { get; set; }
    }
}
