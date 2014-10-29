using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace $rootnamespace$
{
    public class Startup
    {
        static private void copyAddin()
        {
            if (!Directory.Exists("addins"))
            {
                Directory.CreateDirectory("addins");
            }

            File.Copy("NSpecAddin.dll", Path.Combine("addins", "NSpecAddin.dll"), true);
        }

        [STAThread]
        static public void Main(string[] args)
        {
            copyAddin();

            NUnit.Gui.AppEntry.Main(new string[]
            {
                "Test.nunit"
            });
        }
    }
}
