﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Nuget.PackageIndex.VisualStudio
{
    internal static class IServiceProviderExtensions
    {
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
