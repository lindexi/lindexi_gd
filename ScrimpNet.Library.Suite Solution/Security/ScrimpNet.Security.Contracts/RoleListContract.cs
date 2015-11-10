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

namespace ScrimpNet.Security.Contracts
{
    [CollectionDataContract]
    public class RoleListContract :List<RoleContract>
    {
        public bool HasRole(string applicationKey, string roleName)
        {
            return Find(applicationKey, roleName) != null;
        }
        public bool HasActiveRole(string applicationKey, string roleName)
        {
            RoleContract rc = Find(applicationKey, roleName);
            return rc != null && rc.IsActive;
        }
        public RoleContract AddRole(string applicationKey, string roleName)
        {
            RoleContract rc = Find(applicationKey, roleName);

            if (rc == null)
            {
                rc = new RoleContract();
            }

            rc.ApplicationKey = applicationKey; //update names because it might be a casing issue
            rc.RoleName = roleName;
            return rc;
        }

        public RoleContract AddRole(RoleContract role)
        {
            if (role == null) return role;

            RoleContract rc = Find(role.ApplicationKey, role.RoleName);
            if (rc == null)
            {
                this.Add(role);
                return role;
            }
            rc.CopyFrom(role);
            return rc;
        }

        public RoleContract Find(string applicationKey, string roleName)
        {
            return this.Find(c => string.Compare(applicationKey,c.ApplicationKey)==0 && string.Compare(roleName,c.RoleName)==0);
        }

        public int IndexOf(string applicationKey, string roleName)
        {
            RoleContract rc = Find(applicationKey, roleName);
            if (rc == null) return -1;
            return this.IndexOf(rc);
        }
    }
}
