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
using System.Collections.Specialized;
using System.Linq;
using ScrimpNet.Configuration;
using ScrimpNet.Text;
using System.Web.Security;

namespace ScrimpNet.Security.SqlProviders
{
    /// <summary>
    /// A simple facade of Microsoft's SqlRoleProvider that 1) integrates with ScrimpNet's configuration metatokens and 2) provides detailed error messages on configuration failure
    /// </summary>
    public class ScrimpNetSqlRoleProvider : SqlRoleProvider
    {

        public ScrimpNetSqlRoleProvider()
            : base()
        {


        }

        public override string ApplicationName { get; set; }
        public string ConnectionStringName { get; set; }
        public string ConnectionString { get; set; }

        public override void Initialize(string name, NameValueCollection config)
        {
            foreach (var cfgKey in config.AllKeys)
            {
                config[cfgKey] = ConfigManager.ResolveValueSetting(config[cfgKey]);
            }

            ConnectionStringName = config["connectionStringName"];
            System.Configuration.ConnectionStringSettings  cnString = System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName];
            if (cnString == null)
            {
                ExceptionFactory.Throw<System.Configuration.ConfigurationErrorsException>("Unable to find connection key '{0}' in <connectionStrings> section{1}{2}",
                    ConnectionStringName, Environment.NewLine, getConfigMessage(config));
            }
            else
            {
                ConnectionString = cnString.ConnectionString;
            }


            base.Initialize(name, config);  //this erases all values in 'config'

        }

        protected string getConfigMessage(NameValueCollection config)
        {
            string retVal = "";
            retVal = this.GetType().FullName + " Configuration" + Environment.NewLine;
            retVal += @"Details can be found at http://msdn.microsoft.com/en-us/library/system.web.security.sqlroleprovider.aspx" + Environment.NewLine;
            retVal += @"<add name=""some name for your provider (required)""" + Environment.NewLine;
            retVal += @"   type=""fully qualified typename[,assemblyName] (required)""" + Environment.NewLine;
            retVal += @"   connectionStringName=""db connection in <connectionStrings> (required)""" + Environment.NewLine;
            retVal += @"   applicationName=""unique name of application. Often '/' (required)""" + Environment.NewLine;
            retVal += @"/>";
            retVal += Environment.NewLine;
            retVal += "Configured values..." + Environment.NewLine;
            foreach (var key in config.AllKeys)
            {
                retVal += TextUtils.StringFormat(@"{0} = ""{1}""",
                    key, config[key]);
                retVal += Environment.NewLine;
            }
            return retVal;
        }

        protected T verifyKey<T>(string configKey, NameValueCollection config, bool isRequired)
        {
            var key = config.AllKeys.FirstOrDefault(c => string.Compare(c, configKey, false) == 0);
            if (key == null)
            {
                if (isRequired == true)
                {
                    ExceptionFactory.Throw<System.Configuration.ConfigurationErrorsException>("Unable to find required key '{0}' for {1}.{2}{3}",
                        configKey, this.GetType().FullName, Environment.NewLine, getConfigMessage(config));
                }
                else
                {
                    return default(T);
                }
            }

            T result;
            if (Transform.TryConvert<T>(config[configKey], out result) == false)
            {
                ExceptionFactory.Throw<System.Configuration.ConfigurationErrorsException>("Unable to convert key '{0}' for {1}.{2}{3}",
                    configKey, this.GetType().FullName, Environment.NewLine, getConfigMessage(config));
            }
            return result;
        }
    }
}
