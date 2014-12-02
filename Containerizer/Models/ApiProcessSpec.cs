using System;

namespace Containerizer.Models
{
    public class ApiProcessSpec
    {
        public string Path { get; set; }
        public string[] Args { get; set; }

        public string Arguments()
        {
            if (Args == null) { return null; }

            return String.Join(" ", Args);
        }
    }
}