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
using System.Windows.Input;
using System.Reflection;

namespace ScrimpNet.Presentation
{
    /// <summary>
    /// Class the encapsulates logic for closing a window and is the base class for many view models
    /// </summary>
    public abstract class ViewModelClosableWindowBase:ViewModelWindowBase, IClosableWindow
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayName">Unique name for this window</param>
        public ViewModelClosableWindowBase(string displayName)
            : this(displayName, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayName">Unique name for this window</param>
        /// <param name="closeCommand">Command to execute when close is requested</param>
        public ViewModelClosableWindowBase(string displayName, ViewModelCommandBase closeCommand):base()
        {            
            base.DisplayName = displayName??this.GetType().FullName+"::ClosableWindow";
            if (closeCommand == null)
            {
                SimpleCommand cmd = new SimpleCommand(this.GetType().FullName+"::CloseWindow", closeWindow);
                this.CloseCommand = cmd;
            }
            else
            {
                base.Commands.Add(closeCommand);
                this.CloseCommand = closeCommand; //aliased reference for convenience only
            }
        }

        private void closeWindow(object sender)
        {
            if (RequestClose != null)
            {
                RequestClose();
            }
        }
        /// <summary>
        /// Command to execute when close is requested
        /// </summary>
        public ICommand CloseCommand { get; set; }
  
        /// <summary>
        /// Generally set by object outside of this form but is called from inside the form
        /// </summary>
        /// <remarks>
        /// <code>
        ///    viewModel.RequestClose += delegate
        ///    {
        ///         window.Close();
        ///    };
        /// </code>
        /// </remarks>
        public event RequestCloseHandler RequestClose;

    }

    /// <summary>
    /// Method to call when window wants to close
    /// </summary>
    public delegate void RequestCloseHandler();
}
