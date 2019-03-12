using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Activation;
using ScrimpNet.Configuration;

namespace ScrimpNet.ServiceModel
{
    /// <summary>
    /// Custom host factory to use when hosting WCF on shared hosting site.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Addresses: 'The collection already contains an address with scheme Http'
    /// </para>
    /// </remarks>
    /// <example>
    /// <![CDATA[ 
    /// First, open your .svc file. Add the "Factory" attribute to the ServiceHost declaration, replacing the web project name with your own: 
    /// <%@ ServiceHost Language="C#" Debug="true" Service="Concrete.Service.Class"
    ///    Factory="ScrimpNet.ServiceModel.CustomHostFactory" %>
    /// ]]>
    /// </example>
    [Citation("http://www.itscodingtime.com/itscodingtime/post/Installing-a-WCF-Service-to-GoDaddy.aspx")]
    class WcfSharedHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            // If more than one base address exists then return the second address,
            // otherwise return the first address
            if (baseAddresses.Length > 1) //this returns the second address in list.  Might need to change depending on hosting provider
            {
                return new ServiceHost(serviceType, baseAddresses[1]);
            }
            else
            {
                var serviceUri = new Uri(ConfigManager.AppSetting<string>("ScrimpNet.ServiceModel.HostUri"));  //fully qualified name to location of service in shared hosted environment; often might be a full file path including .svc
                                                    //NOTE:  We check for existance of this .config entry even in non-production environments so
                                                    //       missing configuration is caught before being deployed to shared host
                var webServiceAddress = baseAddresses[0].ToString().Contains("localhost") ? baseAddresses[0] : serviceUri;
                return new ServiceHost(serviceType, webServiceAddress);
            }
        }
    }

    class WcfSharedHost : ServiceHost
    {
        public WcfSharedHost(Type serviceType, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        { 
        
        }
    }
}
