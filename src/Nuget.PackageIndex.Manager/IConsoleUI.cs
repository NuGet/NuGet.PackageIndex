namespace Nuget.PackageIndex.Manager
{   
    internal interface IConsoleUI
    {
        void WriteIntro(string message);
        void WriteImportantLine(string message);
        void WriteHighlitedLine(string message);
        void WriteNormalLine(string message);
    }
}
