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

namespace ScrimpNet.Presentation
{
    /// <summary>
    /// Base class for WPF ViewModel commands
    /// </summary>
    public abstract class ViewModelCommandBase : ViewModelObjectBase, ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayName">Unique name of this command</param>
        /// <param name="execute">Method/code to run when command is executed</param>
        public ViewModelCommandBase(string displayName, Action<object> execute)
            : this(displayName, execute, null)
        {

        }

       

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayName">Unique name of this command</param>
        /// <param name="execute">Method/code to run when command is executed</param>
        /// <param name="canExecute">Method/code to run to determine if this command can execute </param>
        public ViewModelCommandBase(string displayName, Action<object> execute, Predicate<object> canExecute):base(displayName)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        private bool? _currentExecuteState = null; //state not set

        /// <summary>
        /// ICommand. Returns true if this command can execute based on the state of the object.
        /// NOTE: If execution code is not defined, then *always* return true
        /// </summary>
        /// <param name="objectState">Object to supply to canExecute delegate that helps the delegate determine if the command can execute</param>
        /// <returns></returns>
        public bool CanExecute(object objectState)
        {
            if (_canExecute == null) return true;
            bool executeState = _canExecute(objectState);
            if (_currentExecuteState.HasValue == false) //first time so initialize state
            {
                _currentExecuteState = executeState;
            }
            
            if ((_currentExecuteState != executeState) ) //state changed
            {
                _currentExecuteState = executeState;
                
              //need to call CanExecuteChanged
            }
            return _currentExecuteState.GetValueOrDefault(true); //if null return TRUE
        }        

        /// <summary>
        /// Trigger a block of code associated with this command
        /// </summary>
        /// <param name="parameter">Data to pass into command execution code</param>
        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
            }
        }

        /// <summary>
        /// Force form to load command status
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        

    }
}
