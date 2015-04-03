using System;
using System.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

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

        ~VsServices()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
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
                    Debug.WriteLine(e.Message); // do nothing for now, log?                    
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
