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
using System.Diagnostics;
using System.Web.Configuration;
using System.Reflection;
using ScrimpNet.Diagnostics;

namespace ScrimpNet.Reflection
{
    /// <summary>
    /// Get value of AssemblyXXX attributes set for this assembly
    /// </summary>
    public partial class AssemblyVersion
    {

        private static string getRootNamespace()
        {
            string namespaceName = (new StackTrace()).GetFrame(0).GetMethod().ReflectedType.Namespace;
            string[] namespaceParts = namespaceName.Split(new char[] { '.' });
            if (namespaceParts != null && namespaceParts.Length > 0)
            {
                return namespaceParts[0] + ".";
            }
            return "";
        }


        private CallerInfo CallerInfoGet(StackTrace st)
        {
            try
            {
                StackFrame[] frames = st.GetFrames();

                for (int frameId = 0; frameId < frames.Length; frameId++)
                {
                    Type frameMethod = frames[frameId].GetMethod().ReflectedType;
                    string frameNamespace = frameMethod.Namespace;
                    if (frameNamespace != null)
                    {
                        bool isExcludedNamespace = false;
                        for (int y = 0; y < _excludedNamespaces.Length; y++)
                        {
                            if (frameNamespace.StartsWith(_excludedNamespaces[y], StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                isExcludedNamespace = true;
                                break;
                            }
                        }
                        if (isExcludedNamespace == false)
                        {
                            return new CallerInfo(frames[frameId]);
                        }
                    }
                    if (frameNamespace == null)
                    {
                        return new CallerInfo(frames[frameId]); //null namespace is an asp page
                    }
                }

                if (frames.Length > 0) //we've walked the entire frame stack
                {
                    return new CallerInfo(frames[frames.Length - 1]); //get top most calling assembly
                }
            }
            catch (Exception ex)
            {
                Log.LastChanceLog(ex);
            }
            // nothing found so return default empty object
            return new CallerInfo();
        }

        /// <summary>
        /// Get callerInfo of currently executing method
        /// </summary>
        /// <returns>Hydrated caller info object</returns>
        public CallerInfo CallerInfoGet()
        {
            return CallerInfoGet(new StackTrace());
        }
    }
}
