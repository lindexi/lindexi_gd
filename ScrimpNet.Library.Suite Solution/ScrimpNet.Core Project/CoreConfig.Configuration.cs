using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace ScrimpNet
{
    public static partial class CoreConfig
    {
        /// <summary>
        /// Values to bootstrap configuration providers
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Fully qualified name of assembly ('type','assembly') that will provide external configuration data to application. Assembly must implement IInternalConfigSystem. (Key: ScrimpNet.Configuration.ExternalSource)
            /// </summary>
            /// <remarks>
            /// Used ConfigurationManager reference instead of ScrimpNet.ConfigManager since this value is read at library bootstrapping time and ConfigManager isn't fully initialized
            /// </remarks>
            public static string ExternalSource
            {
                get
                {
                    string configSource = ConfigurationManager.AppSettings["ScrimpNet.Configuration.ExternalSource"];
                    if (configSource == null || string.IsNullOrEmpty(configSource) == true)
                    {
                        ScrimpNet.Diagnostics.Log.LastChanceLog(Diagnostics.MessageLevel.Warning, "Unable to find <appSettings> key 'ScrimpNet.Configuration.ExternalSource'.  External configuration source might be disabled");
                    }
                    return configSource;
                }
            }

            /// <summary>
            /// Url for connecting WcfConfigSystem to WCF server (Required: ScrimpNet.Configuration.WcfServiceUrl)
            /// </summary>
            /// <remarks>
            /// Used ConfigurationManager reference instead of ScrimpNet.ConfigManager since this value is read at library bootstrapping time and ConfigManager isn't fully initialized
            /// </remarks>
            public static string WcfUrl
            {
                get
                {
                    string url = ConfigurationManager.AppSettings["ScrimpNet.Configuration.WcfServiceUrl"];
                    if (url == null || string.IsNullOrEmpty(url) == true)
                    {
                        string msg = "Unable to find <appSettings> key 'ScrimpNet.Configuration.WcfServiceUrl'";
                        Diagnostics.Log.LastChanceLog(Diagnostics.MessageLevel.Error, msg);
                        throw new ConfigurationErrorsException(msg);
                    }
                    return url;
                }
            }

            /// <summary>
            /// String of characters the delimits the beginning of a meta data lookup field (Constant: '{%')
            /// </summary>
            public static string StartToken
            {
                get
                {
                    return "{%";
                }
            }
            /// <summary>
            /// String of characters the delimits the ending of a meta data lookup field (Constant: '%}')
            /// </summary>
            public static string StopToken
            {
                get
                {
                    return "%}";
                }
            }
        }
    }
}
