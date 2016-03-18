using System;
using System.IO;

namespace SubTypeReferencesAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args?.Length != 1) return;
            if (!File.Exists(args[0])) return;

            var analyserControl = new SubTypeReferencesFinder(args[0], Console.WriteLine, Console.ReadLine);
            
            analyserControl.StartSearch();

            Console.ReadKey();
        }
    }
}