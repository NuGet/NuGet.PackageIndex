using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// It should be used in the code that works with Vs UI/Com components to raise an attention 
    /// if such code is executing on non UI thread
    /// </summary>
    internal static class UIThreadHelper
    {
        public static void VerifyOnUIThread([CallerMemberName] string memberName = "")
        {
            if (!UnitTestHelper.IsRunningUnitTests)
            {
                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread(memberName);
                }
                catch
                {
                    Debug.Fail("Call made on the Non-UI thread by " + memberName);
                    throw;
                }
            }
        }
    }
}
