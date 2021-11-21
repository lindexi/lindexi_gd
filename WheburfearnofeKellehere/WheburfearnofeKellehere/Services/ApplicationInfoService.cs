using System;
using System.Diagnostics;
using System.Reflection;

using WheburfearnofeKellehere.Contracts.Services;

namespace WheburfearnofeKellehere.Services
{
    public class ApplicationInfoService : IApplicationInfoService
    {
        public ApplicationInfoService()
        {
        }

        public Version GetVersion()
        {
            // Set the app version in WheburfearnofeKellehere > Properties > Package > PackageVersion
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var version = FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
            return new Version(version);
        }
    }
}
