namespace Nuget.PackageIndex.Manager
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var indexManager = new PackageIndexManager();
            indexManager.Run(args);
        }
    }
}
