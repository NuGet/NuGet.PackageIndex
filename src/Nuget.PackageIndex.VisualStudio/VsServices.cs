// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// TODO remove this hack after RC
    /// </summary>
    internal class VsServices : IDisposable
    {
        private static VsServices s_instance = new VsServices();
        public static VsServices Instance
        {
            get
            {
                return s_instance;
            }
        }

        private bool _disposed;
        private DTEEvents _dteEvents;

        public void Initialize()
        {
            var container = ServiceProvider.GlobalProvider.GetService<IComponentModel, SComponentModel>();
            var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
            Debug.Assert(dte != null, "Couldn't get the DTE. Crash incoming.");

            var events = (Events2)dte.Events;
            if (events != null)
            {
                _dteEvents = events.DTEEvents;
                Debug.Assert(_dteEvents != null, "Cannot get DTEEvents");

                _dteEvents.OnBeginShutdown += OnBeginShutdown;
            }
        }

        private void OnBeginShutdown()
        {
            PackageIndexFactory.LocalIndexCancellationTokenSource.Cancel();
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    if (_dteEvents != null)
                    {
                        _dteEvents.OnBeginShutdown -= OnBeginShutdown;
                    }  
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString()); // do nothing for now, log?                    
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
