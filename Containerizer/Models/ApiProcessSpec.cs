using System;

namespace Containerizer.Models
{
    public class ApiProcessSpec
    {
        public string Path { get; set; }
        public string[] Args { get; set; }

        public string Arguments()
        {
            return String.Join(" ", Args);
        }
    }
}