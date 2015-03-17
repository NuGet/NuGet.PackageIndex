using System;

namespace Nuget.PackageIndex.Manager
{   
    internal class ConsoleUI : IConsoleUI
    {
        public void WriteIntro(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void WriteImportantLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void WriteHighlitedLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void WriteNormalLine(string message)
        {
            Console.WriteLine(message);
        }       
    }
}
