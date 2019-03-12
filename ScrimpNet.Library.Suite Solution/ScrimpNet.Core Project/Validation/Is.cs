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

namespace ScrimpNet
{
    /// <summary>
    /// Validate data but do not throw an exception on failure.  Just return false
    /// </summary>
    public partial class Is
    {
       
            private static readonly Validator _validator = new Validator();

        /// <summary>
        /// Default constructor
        /// </summary>
            static Is()
            {
                _validator.ClearOnUnexpected();
                _validator.OnUnexpectedEvents += _defaultUnexpectedHandler;
            }
            /// <summary>
            /// Method that is called by default when validation fails.  Overwrite this to customize response
            /// </summary>
            /// <param name="eventArgs">Information about the value that is being Validated</param>
            private static void _defaultUnexpectedHandler(GuardEventArgs eventArgs)
            {
                //don't do anyting since this class returns value through method calls
            }
       
    }
}
