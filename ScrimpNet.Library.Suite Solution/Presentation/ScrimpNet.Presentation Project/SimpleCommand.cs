/**
/// ScrimpNet.Core Library
/// Copyright (c) 2005-2010
///
/// This module is Copyright (c) 2005-2010 Steve Powell
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

namespace ScrimpNet.Presentation
{
    /// <summary>
    /// Generic command that handles many simple use-cases in ViewModels
    /// </summary>
   public class SimpleCommand : ViewModelCommandBase 
    {
       /// <summary>
       /// Constructor
       /// </summary>
       /// <param name="display">Unique identifieer for this command</param>
       /// <param name="action">Code block to execute by WPF framework.  CanExecute is always TRUE</param>
       public SimpleCommand(string display, Action<object> action)
           : base(display, action)
       {
       }

       /// <summary>
       /// Constructor
       /// </summary>
       /// <param name="displayName">Unique identifier for this command</param>
       /// <param name="execute">Code block to execute by WPF framework.</param>
       /// <param name="canExecute">Code block to execute before executing to see if command is allowed to execute within ViewModel's current state</param>
       public SimpleCommand(string displayName, Action<object> action, Predicate<object> canExecute)
           : base(displayName, action, canExecute)
       {
       }

    }
}
