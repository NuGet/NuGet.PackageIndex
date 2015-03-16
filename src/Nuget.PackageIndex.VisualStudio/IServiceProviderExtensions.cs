using System;

namespace Nuget.PackageIndex.VisualStudio
{
    internal static class IServiceProviderExtensions
    {
        /// <summary>
        /// Returns the service specified
        /// </summary>
        public static InterfaceType GetService<InterfaceType>(this IServiceProvider sp) where InterfaceType : class
        {
            InterfaceType service = null;

            try
            {
                service = sp.GetService(typeof(InterfaceType)) as InterfaceType;
            }
            catch
            {
            }
            return service;
        }

        /// <summary>
        /// Returns the specified interface from the service. This is useful when the service and interface differ
        /// </summary>
        public static InterfaceType GetService<InterfaceType, ServiceType>(this IServiceProvider sp)
            where InterfaceType : class
            where ServiceType : class
        {
            InterfaceType service = null;

            try
            {
                service = sp.GetService(typeof(ServiceType)) as InterfaceType;
            }
            catch
            {
            }

            return service;
        }
    }
}
