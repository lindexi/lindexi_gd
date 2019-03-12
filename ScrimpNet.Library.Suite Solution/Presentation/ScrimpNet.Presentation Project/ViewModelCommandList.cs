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
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ScrimpNet.Presentation
{
    /// <summary>
    /// List of WPF commands for ViewModel support
    /// </summary>
    public class ViewModelCommandList :ObservableCollection<ViewModelCommandBase>
    {
        /// <summary>
        ///  Stores a new command in command registry with a specific action and execution validation code blocks
        /// </summary>
        /// <param name="displayName">Unique string name for this command</param>
        /// <param name="execute">Code block to execute when command is executed</param>
        /// <param name="canExecute">Method to call when WPF interrogates the command to see if it can execute</param>
        public void Add(string displayName, Action<object> execute, Predicate<object> canExecute)
        {
            Add(new SimpleCommand(displayName,execute,canExecute));
        }

        /// <summary>
        /// Stores a new command in command registry with a specific action and CanExecute always TRUE
        /// </summary>
        /// <param name="displayName">Unique string name for this command</param>
        /// <param name="execute">Action to take when the command's 'Execute' method is called</param>
        public void Add(string displayName, Action<object> execute)
        {
            Add(new SimpleCommand(displayName, execute, (object o) => { return true; }));
        }

        
        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="commandKey">String key that was used to add command to collection</param>
        /// <returns>Command that was previously registered in collection</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="commandKey"/> is not found in list</exception>
        public ViewModelCommandBase this[string commandKey]
        {
            get
            {
                var retVal = this.FirstOrDefault<ViewModelCommandBase>(c => string.Compare(c.DisplayName, commandKey) == 0);
                if (retVal == null)
                {
                    throw new IndexOutOfRangeException(string.Format("Unable to find command with key: '{0}'", commandKey));
                }
                return retVal;
            }
        }
    }
}
