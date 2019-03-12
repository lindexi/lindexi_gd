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
using System.Windows;

namespace ScrimpNet.Presentation.Demo.Models
{
    /// <summary>
    /// View that demonstrates simple behavior of M-V-VM ScrimpNet.Presentation library.  This demo
    /// is not intended to demostrate WPF techniques and may not follow best practices
    /// </summary>
    /// <remarks>
    /// Production code such as comments, logging, exception handling, authorization, etc not included for
    /// clarity purposes.
    /// </remarks>
    public class MainViewModel : ViewModelClosableWindowBase //<= inheriting from ClosableWindowBase
    {
        private class Command //internal convenience class for storing strings.  This is not necessary and is author's personal style
        {
            public const string LeftButton = "LeftButton";
            public const string RightButton = "RightButton";
            public const string ResetButton = "ResetButton";
        }

        public MainViewModel()
            : base(typeof(MainViewModel).FullName + "::viewModel") //<= ViewModel objects must have a textual identifier
        {
            Commands.Add(Command.LeftButton, leftClick, leftCanExecute);  //<= adding commands to command registry
            Commands.Add(Command.RightButton, rightClick, rightCanExecute);
            Commands.Add(Command.ResetButton, resetClick, resetCanExecute);
        }

        #region ButtonCommands

        public ICommand LeftCommand //<= ICommand that is bound to view button
        {
            get
            {
                return Commands[Command.LeftButton];
            }
        }
        public void leftClick(object obj) //<= action to take when button is clicked.  Name is not relevant here.  Method can be called anything.
        {
            Counter++;
        }
        public bool leftCanExecute(object obj) //<= filter to see if command is allowed to execute.  If not, button is disabled automatically
        {
            return Counter % 2 != 0 && Counter < 10; //<=dummy logic enable left button if Odd
        }
        public ICommand RightCommand
        {
            get
            {
                return Commands[Command.RightButton];
            }
        }
        private void rightClick(object obj)
        {
            Counter++;
        }
        private bool rightCanExecute(object obj)
        {
            return Counter % 2 == 0 && Counter < 10; //<=dummy logic enable right button if even
        }
        public ICommand ResetCommand
        {
            get
            {
                return Commands[Command.ResetButton];
            }
        }
        private void resetClick(object obj) //<=setting changing values in model
        {
            Counter = 0;
            FontSize = 12;
        }
        private bool resetCanExecute(object obj)
        {
            return Counter >= 10;
        }
        #endregion ButtonCommands

        #region Member Fields For View
        private int _counter = 0;
        public int Counter //<= this property is displayed in the counter box
        {
            get
            {
                return _counter;
            }
            set
            {
                if (_counter != value)
                {
                    _counter = value;
                    FontSize += _counter;
                    RaisePropertyChanged("Counter"); //<= tell WPF to refresh view
                }
            }
        }
        private int _fontSize = 12;
        public int FontSize //<= this property determines the currently active font size for counter box
        {
            get
            {
                return _fontSize;
            }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    RaisePropertyChanged("FontSize");
                }
            }
        }
        #endregion Member Fields For View

        #region Validation Logic

        private string _errorMsg = null;
        public string ErrorMessage //<= this message displays over the counter box
        {
            get
            {
                return _errorMsg;
            }
            set
            {
                if (string.Compare(_errorMsg, value) != 0)
                {
                    _errorMsg = value;
                    RaisePropertyChanged("ErrorMessage");
                }
            }
        }

        public override string this[string propertyName] //<= activate internal IDataErrorInfo property and throw simulated error
        {
            get
            {
                switch (propertyName)
                {
                    case "FontSize":
                        if (Counter >= 10)
                        {
                            ErrorMessage = "Counter must not exceed 10.  Use RESET button to start over";
                            return "Counter must not exceed 10";
                        }
                        break;
                }
                ErrorMessage = null; //<= no error condition
                return null;
            }
        }
        #endregion Validation Logic

    }
}
