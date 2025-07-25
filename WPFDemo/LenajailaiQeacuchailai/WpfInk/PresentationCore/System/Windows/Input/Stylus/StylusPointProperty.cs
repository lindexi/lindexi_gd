﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using WpfInk.PresentationCore.System.Windows.Ink;

namespace WpfInk.PresentationCore.System.Windows.Input.Stylus
{
    /// <summary>
    /// StylusPointProperty
    /// </summary>
    internal class StylusPointProperty
    {
        /// <summary>
        /// Instance data
        /// </summary>
        private Guid                    _id;
        private bool                    _isButton;

        /// <summary>
        /// StylusPointProperty
        /// </summary>
        /// <param name="identifier">identifier</param>
        /// <param name="isButton">isButton</param>
        public StylusPointProperty(Guid identifier, bool isButton)
        {
            Initialize(identifier, isButton);
        }

        /// <summary>
        /// StylusPointProperty
        /// </summary>
        /// <param name="stylusPointProperty"></param>
        /// <remarks>Protected - used by the StylusPointPropertyInfo ctor</remarks>
        protected StylusPointProperty(StylusPointProperty stylusPointProperty)
        {
            ArgumentNullException.ThrowIfNull(stylusPointProperty);
            Initialize(stylusPointProperty.Id, stylusPointProperty.IsButton);
        }

        /// <summary>
        /// Common ctor helper
        /// </summary>
        /// <param name="identifier">identifier</param>
        /// <param name="isButton">isButton</param>
        private void Initialize(Guid identifier, bool isButton)
        {
            //
            // validate isButton for known guids
            //
            if (StylusPointPropertyIds.IsKnownButton(identifier))
            {
                if (!isButton)
                {
                    //error, this is a known button
                    throw new ArgumentException(SR.InvalidIsButtonForId, nameof(isButton));
                }
            }
            else
            {
                if (StylusPointPropertyIds.IsKnownId(identifier) && isButton)
                {
                    //error, this is a known guid that is NOT a button
                    throw new ArgumentException(SR.InvalidIsButtonForId2, nameof(isButton));
                }
            }

            _id = identifier;
            _isButton = isButton;
        }

        /// <summary>
        /// Id
        /// </summary>
        public Guid Id 
        {
            get { return _id; }
        }

        /// <summary>
        /// IsButton
        /// </summary>
        public bool IsButton
        {
            get { return _isButton; }
        }

        /// <summary>
        /// Returns a human readable string representation
        /// </summary>
        public override string ToString()
        {
            return "{Id=" +
                StylusPointPropertyIds.GetStringRepresentation(_id) +
                ", IsButton=" +
                _isButton.ToString(CultureInfo.InvariantCulture) +
                "}";
        }
    }
}
