namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Used to skip VS UI interaction when running unit tests
    /// </summary>
    internal static class UnitTestHelper
    {
        public static bool IsRunningUnitTests { get; set; }
    }
}
