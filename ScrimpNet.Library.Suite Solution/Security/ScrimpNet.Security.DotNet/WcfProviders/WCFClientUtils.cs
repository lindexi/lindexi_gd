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
using ScrimpNet.ServiceModel.BaseContracts;
using ScrimpNet.ServiceModel;

namespace ScrimpNet.Security.WcfProviders
{
   public static class WcfClientUtils
    {
       static string _sessionToken;
        /// <summary>
        /// Attempt to contact service end point and return a session token (Dummy value.  Place holder for later implementation)
        /// </summary>
        public static string SessionToken
        {
            get
            {
                if (string.IsNullOrEmpty(_sessionToken) == true)
                {
                    _sessionToken = Guid.NewGuid().ToString();
                }
                return _sessionToken;
            }
        }

       /// <summary>
       /// From &lt;appSettings&gt; ScrimpNet.Application.Key
       /// </summary>
        public static string ApplicationKey
        {
            get
            {
                return CoreConfig.ApplicationKey;
            }
        }

        internal static void VerifyParameter(string paramName, string value)
        {
            if (value == null) throw new ArgumentNullException(paramName);
            if (string.IsNullOrEmpty(value)) throw new ArgumentException(paramName + " cannot be null or empty", paramName);
        }

        internal static void VerifyParameter(string paramName, string[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentNullException(paramName, paramName + "Must not be null or empty");
            }
            for(int x=0;x<values.Length;x++)
            {
                VerifyParameter(string.Format("{0}[{1}]",paramName,x), values[x]);
            }
        }


    }
}
