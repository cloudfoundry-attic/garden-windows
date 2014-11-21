using System;
using System.IO;
using NUnit.Gui;

namespace Containerizer.Tests
{
    public class Startup
    {
        private static void copyAddin()
        {
            if (!Directory.Exists("addins"))
            {
                Directory.CreateDirectory("addins");
            }

            File.Copy("NSpecAddin.dll", Path.Combine("addins", "NSpecAddin.dll"), true);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            copyAddin();

            AppEntry.Main(new[]
            {
                "Test.nunit"
            });
        }
    }
}