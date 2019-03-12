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
using System.Configuration.Internal;
using ScrimpNet.ServiceModel;
using ScrimpNet.Diagnostics;
using System.Collections.Specialized;
using System.ServiceModel;
using System.Threading;
using System.Configuration;

namespace ScrimpNet.Configuration.ConfigSystems
{
    /// <summary>
    /// Used to replace ConfigurationManager's default configuration system. 
    /// This is a WCF provider by example.
    /// </summary>
    public sealed class ConfigWcfConfigSystem : IInternalConfigSystem, IDisposable
    {
        readonly IInternalConfigSystem _nativeConfigSystem;
        Log _log = Log.NewLog(typeof(ConfigWcfConfigSystem));

        IWcfConfigurationService _svc;

        /// <summary>
        /// Default constructor.  Requires configuration key: ScrimpNet.Configuration.WcfServiceUrl
        /// </summary>
        public ConfigWcfConfigSystem():this(CoreConfig.Configuration.WcfUrl)
        {

        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="wcfConfigurationServiceUrl">Fully qualified url including .svc file</param>
        public ConfigWcfConfigSystem(string wcfConfigurationServiceUrl)
        {
            _svc = WcfClientFactory.Create<IWcfConfigurationService>(wcfConfigurationServiceUrl);
        }

        /// <summary>
        /// Specialized constructor so this system can be used outside of ConfigurationManager
        /// </summary>
        /// <param name="wcfConfigurationServiceUrl">Fully qualified url including .svc file</param>
        /// <param name="endPointConfigurationName">Specific section within WCF configuration</param>
        public ConfigWcfConfigSystem(string wcfConfigurationServiceUrl, string endPointConfigurationName)
        {
            _svc = WcfClientFactory.Create<IWcfConfigurationService>(wcfConfigurationServiceUrl, endPointConfigurationName);
        }

        /// <summary>
        /// Getall settings (inlcude all variants) for a specific applciation
        /// </summary>
        /// <param name="applicationKey">Existing application that has settings</param>
        /// <returns>List of settings or empty list of non-found</returns>
        public List<ConfigSetting> GetAllSettings(string applicationKey)
        {
            using (_log.NewTrace())
            {
                try
                {
                    return _svc.GetAllSettings(applicationKey);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    throw;
                }
            }
        }
        /// <summary>
        /// Constructor used by the internal configuration manager
        /// </summary>
        /// <param name="baseconf">Native instance of configuration system</param>
        internal ConfigWcfConfigSystem(IInternalConfigSystem baseconf)
        {
            _nativeConfigSystem = baseconf;
            _svc = WcfClientFactory.Create<IWcfConfigurationService>(CoreConfig.Configuration.WcfUrl);
            
        }
        private static ReaderWriterLockSlim _serviceLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static NameValueCollection _appSettings = null;

        /// <summary>
        /// This is central processing logic.  To support additional sections, you extend the switch statement
        /// </summary>
        /// <param name="configSection">Name of first level configuration section</param>
        /// <returns>One of the strongly typed configuration classes</returns>
        public object GetSection(string configSection)
        {
            switch (configSection)
            {
                case "appSettings":
                    loadAndMergeAppSettings();
                    return _appSettings;
                case "connectionStrings":
                    loadAndMergeConnectionStrings();                    
                    return _nativeConfigSystem.GetSection(configSection);
                default:
                    return _nativeConfigSystem.GetSection(configSection);
            }
        }

        private void loadAndMergeConnectionStrings()
        {
            ConnectionStringsSection s = _nativeConfigSystem.GetSection("connectionStrings") as ConnectionStringsSection;                  
        }

        private NameValueCollection loadAndMergeAppSettings()
        {
            try
            {
                _serviceLock.EnterUpgradeableReadLock();
                if (_appSettings != null) return _appSettings;
                try
                {
                    _serviceLock.EnterWriteLock();
                    if (_appSettings != null) return _appSettings;

                    NameValueCollection col = new NameValueCollection();
                    var wcfList = _svc.GetAllSettings(CoreConfig.ApplicationKey);  // read all application keys from configuration service
                    foreach (var setting in wcfList)
                    {
                        col.Add(setting.SettingKey, "value 1");
                    }

                    //TODO merge settings from external source into .config source

                    NameValueCollection nvc = _nativeConfigSystem.GetSection("appSettings") as NameValueCollection;

                    foreach (var item in nvc.AllKeys)
                    {
                        col.Add(item, nvc[item]);
                    }

                    _appSettings = col;
                    return _appSettings;
                }
                finally
                {
                    _serviceLock.ExitWriteLock();
                }
            }
            finally
            {
                _serviceLock.ExitUpgradeableReadLock();
            }
        }
        /// <summary>
        /// Required logic when creating a custom custom configuration system
        /// </summary>
        /// <param name="sectionName">First level configuration section</param>
        public void RefreshConfig(string sectionName)
        {
            switch (sectionName)
            {
                case "appSettings":
                    loadAndMergeAppSettings();
                    return;
                case "connectionStrings":
                    _nativeConfigSystem.RefreshConfig(sectionName);
                    return;
                default:
                    _nativeConfigSystem.RefreshConfig(sectionName);
                    return;
            }
        }
        /// <summary>
        /// Requied when creating a custom configuration system
        /// </summary>
        public bool SupportsUserConfig
        {
            get { return _nativeConfigSystem.SupportsUserConfig; }
        }
        protected void dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_svc != null)
                {
                    WcfClientFactory.CloseAndDispose((IClientChannel)_svc);
                }
            }
        }
        public void Dispose()
        {
            dispose(true);
        }
        ~ConfigWcfConfigSystem()
        {
            dispose(false);
        }
    }
}
