using System;

namespace Nuget.PackageIndex.Manager
{   
    static class ConsoleHelper
    {
        public static void WriteIntro(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void WriteImportantLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void WriteHighlitedLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void WriteNormalLine(string message)
        {
            Console.WriteLine(message);
        }

        public static string ReadLine()
        {
            return Console.ReadLine();
        }        
    }
}
