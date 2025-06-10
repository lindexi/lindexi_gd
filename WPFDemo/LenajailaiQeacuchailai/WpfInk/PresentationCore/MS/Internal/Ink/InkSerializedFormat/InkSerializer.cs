// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define OLD_ISF

using MS.Utility;
using System;
using System.Security;
using System.Windows;
//using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using MS.Internal.IO.Packaging;
using WpfInk.PresentationCore.System.Windows;

namespace MS.Internal.Ink.InkSerializedFormat
{
    internal class StrokeCollectionSerializer
    {
        internal static readonly double AvalonToHimetricMultiplier = 2540.0d / 96.0d;
        internal static readonly double HimetricToAvalonMultiplier = 96.0d / 2540.0d;
    }
}

