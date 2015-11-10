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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScrimpNet;
using System.Reflection;
using System.Data;
using System.Data.SqlClient;
namespace CoreTests
{
    [TestClass]
    public class ExceptionTests
    {
        [TestMethod]
        public void ExpandException_Tests()
        { 
            try
            {
                try
                {
                   
                    using (var cn = new SqlConnection("Integrated Security=SSPI;Persist Security Info=False;User ID=sa;Initial Catalog=DummyDatabase;Data Source=.\\SQLBADSERVER;Timeout=5"))
                    {
                        try
                        {
                      
                            cn.Open();
                        }
                        finally
                        {
                            if (cn.State == ConnectionState.Open)
                            {
                                cn.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                   
                    ex.Data["x"] = 0;
                    ex.Data["y"] = 0;
                  
                    throw new ArgumentException("test of inner exceptions:",ex);
                }
            }
            catch (Exception ex)
            {
                string s = ex.Expand(); //<= inspect this variable to see format of exception message(s)
            }
        }
    }
}
