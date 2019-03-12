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
using System.Net.Mail;

namespace ScrimpNet
{
   public partial class Guard
    {
       /// <summary>
        /// Verify a string contains a valid email address format. Email address must contain user@host.TLD and will fail if any of three components not provided. Does not validate if address is a "live" address.
        /// See <see cref="Validator.EMailValid"/>
       /// </summary>
       /// <param name="email">String containing email address to verify</param>
       /// <returns>True or throws an exception (default Guard behavior) if not valid</returns>
        public static bool EMailValid(string email)
        {
            return _validator.EMailValid(email);
        }

    }
}
