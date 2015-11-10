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
using System.Linq;
using System.Text;

namespace ScrimpNet.Reflection
{
    public partial class AssemblyVersion
    {
        /// <summary>
        /// These namespace(s) will not be included in generated stack traces.  This helps clean up logging and reporting.  During a stack
        /// trace dump, as the stack is unwound it will not report any namespaces until reaching a namespace NOT included here.  After
        /// that, then all namespaces are included in the stack dump. Excluded namespaces='System','Microsoft','ScrimpNet'
        /// </summary>
        private static string[] _excludedNamespaces = new string[] { "System", "Microsoft", "ScrimpNet", getRootNamespace() }; //EXTENSION
    }
}
