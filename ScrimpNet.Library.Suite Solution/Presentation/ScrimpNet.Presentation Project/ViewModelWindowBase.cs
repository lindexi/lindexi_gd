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
    /// Base class for all ViewModels that are bound to some kind of WPF window or frame
    /// </summary>
    public abstract class ViewModelWindowBase:ViewModelObjectBase 
    {

        private ViewModelCommandList _commands = new ViewModelCommandList();

        /// <summary>
        /// List of commands this window is supporting
        /// </summary>
        public ViewModelCommandList Commands
        {
            get
            {
                return _commands;
            }
            set
            {
                _commands = value;
            }
        }
    }
}
