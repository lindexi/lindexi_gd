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
using System.Text;
using System.Text.RegularExpressions;
using ScrimpNet.Diagnostics;


namespace ScrimpNet
{

    /// <summary>
    /// Validate objects being passed into a method.  Note: Expected methods are to be extended as use cases require.
    /// </summary>
    /// <remarks>
    /// This class allows you to easily verify the input objects on your method calls.  By implementing consistent object checking
    /// you can increase the reliability of your code.  This class makes it much easier to incorporate this key discipline
    /// without incurring productivity overhead.
    /// <para><h3>Quick Start</h3></para>
    /// Call Guard() at the start of each public or protected method.  The default behavior is to throw an <see cref="GuardViolationException"/> but this behavior
    /// can easily be extended to add logging, custom message types, or other actions.
    /// <para>
    /// <code>
    /// <![CDATA[ 
    /// public void WriteStringAt(string message, int xPos, int yPos)
    ///     {
    ///         Guard.NotNullOrEmpty(message, "WriteStringAt::message", "ValidationDemo");
    ///         Guard.GreaterThan<int>(xPos, 0, "WriteStringAt::xPos", "ValidationDemo", "xPos {0} must be greater than 0", xPos);
    ///         Guard.Between<int>(yPos, 2, 23, "WriteStringAt::yPos", "ValidationDemo", "objects ({0},{1}) must be between ({2},{3})",
    ///             "yPos", yPos, 2, 23);
    /// 
    ///         // perform function here
    ///     }
    /// ]]>
    /// </code>
    /// </para>
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords")]
    public partial class Guard
    {
        private static readonly Validator _validator = new Validator();
      
      static Guard()
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
            GuardViolationException pe = new GuardViolationException(eventArgs.ApplicationName, eventArgs.Message);
            pe.Data.Add("ValidationDetails", eventArgs);
            throw pe;
        }   
    }
}
