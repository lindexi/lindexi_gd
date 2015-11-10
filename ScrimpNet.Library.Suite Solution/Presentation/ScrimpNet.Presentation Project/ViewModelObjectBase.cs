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
using System.ComponentModel;
using System.Diagnostics;

namespace ScrimpNet.Presentation
{
    /// <summary>
    /// Base class for all ScrimpNet WPF objects.
    /// </summary>
    public abstract class ViewModelObjectBase : IDisposable, INotifyPropertyChanged, IDataErrorInfo
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ViewModelObjectBase() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="displayName">String identifier for this object</param>
        public ViewModelObjectBase(string displayName)
        {
            _displayName = displayName;
        }

        /// <summary>
        /// INotifyPropertyChanged - fired whenever a property changes on class.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise a PropertyChanged event for a specific property.
        /// </summary>
        /// <param name="propertyName">Name of property that is changing</param>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="propertyName"/> doesn't exist in class</exception>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;  //do nothing if no registered listeners

            this.VerifyPropertyName(propertyName);
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged(this, e); //execute any registered listeners for PropertyChanged
        }

        /// <summary>
        /// Verify a property exists using reflection.  
        /// NOTE:  DEBUG Only
        /// </summary>
        /// <param name="propertyName">Name of property to verify</param>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: '" + propertyName + "'";
                if (this.ThrowOnInvalidPropertyName==true)
                {
                    throw new InvalidOperationException(msg);
                }                
            }
        }

        private bool _throwOnInvalidPropertyName = true;
        /// <summary>
        /// Determines how the object should react if an invalid property is sent for notification. Default=true.  DEBUG only.
        /// </summary>
        public bool ThrowOnInvalidPropertyName
        {
            get
            {
                return _throwOnInvalidPropertyName;
            }
            set
            {
                if (value != _throwOnInvalidPropertyName)
                {
                    _throwOnInvalidPropertyName = value;
                    RaisePropertyChanged("ThrowOnInvalidPropertyName");
                }
            }
        }

        private void dispose(bool isDisposing)
        {
            if (isDisposing) //release managed resources
            {
                releaseManagedResources();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called during Dispose(). May be overridden in descendent classes.
        /// Descendent classes should be sure to call base.releaseManagedResources
        /// </summary>
        protected virtual void releaseManagedResources()
        {
            //do nothing in base class
        }

        /// <summary>
        /// Release resources
        /// </summary>
        public void Dispose()
        {
            dispose(true);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~ViewModelObjectBase()
        {
            dispose(false);
        }

        private string _displayName;
        /// <summary>
        /// A textual description of this object
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                if (string.Compare(_displayName, value) != 0)
                {
                    _displayName = value;
                    RaisePropertyChanged("DisplayName");
                }
            }
        }

        /// <summary>
        /// Create string representation of object
        /// </summary>
        /// <returns>DisplayName of ViewModel object</returns>
        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Not available for WPF applications.  WinForms only
        /// </summary>
        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Called by WPF validation framework to ensure validity of object.  Override in descendent 
        /// classes to validate specific class properties. For IDataErrorInfo implementation.
        /// </summary>
        /// <param name="propertyName">Name of class property that is being validated</param>
        /// <returns>NULL if no error or some kind of message if <paramref name="propertyName"/> is in an error state</returns>
        public virtual string this[string propertyName]
        {
            get
            {
                return null; //by default do NOT create an ErrorState.  Orderride method in descendent classes
            }
        }
    }
}
