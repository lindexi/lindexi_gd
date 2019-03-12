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
    [DataContract(Namespace=ContractConfig.ContractVersion)]
    public class RoleContract : ICloneable 
    {
        /// <summary>
        /// Name of role within an application
        /// </summary>
        [DataMember]
        public string RoleName { get; set; }

        /// <summary>
        /// Application containing this role
        /// </summary>
        [DataMember]
        public string ApplicationKey { get; set; }

        private DateTime _effectiveFrom = DateTime.MaxValue;
        /// <summary>
        /// Instant in time when this role becomes effective
        /// </summary>
        [DataMember]
        public DateTime EffectiveFromUtc
        {
            get
            {
                return _effectiveFrom;
            }
            set
            {
                _effectiveFrom = value;
            }
        }

        private DateTime _effectiveTo = DateTime.MinValue;
        /// <summary>
        /// Last instant in time this role is effective
        /// </summary>
        [DataMember]
        public DateTime EffectiveToUtc
        {
            get
            {
                return _effectiveTo;
            }
            set
            {
                _effectiveTo = value;
            }
        }

        private bool _isActive = false;
        /// <summary>
        /// Switch that determines if this role is active
        /// </summary>
        [DataMember]
        public bool IsActive {
            get
            {
                if (_isActive == false) return false;
                long nowTicks = DateTime.UtcNow.Ticks;
                return nowTicks >= EffectiveFromUtc.Ticks && nowTicks <= EffectiveToUtc.Ticks;
            }
            set
            {
                _isActive = value;
            }
        }

        /// <summary>
        /// Create copy of this entry
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RoleContract rc = new RoleContract();
            rc.RoleName = RoleName ;
            rc._effectiveFrom = _effectiveFrom;
            rc._effectiveTo = _effectiveTo;
            rc._isActive = _isActive;
            rc.ApplicationKey = ApplicationKey;
            return rc;
        }

        /// <summary>
        /// Copy fields from <paramref name="role" to this object/>
        /// </summary>
        /// <param name="role">Hydrated role to copy</param>
        /// <returns>This object with new values for fields</returns>
        public RoleContract CopyFrom(RoleContract role)
        {
            _effectiveFrom = role._effectiveFrom;
            _effectiveTo = role._effectiveTo;
            _isActive = role._isActive;
            RoleName = role.RoleName;
            ApplicationKey = role.ApplicationKey;
            return this;
        }
    }
}
