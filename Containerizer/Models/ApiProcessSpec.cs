#region

using System;
using System.Text;

#endregion

namespace Containerizer.Models
{
    public class ApiProcessSpec
    {
        public string Path { get; set; }
        public string[] Args { get; set; }

        public string Arguments()
        {
            if (Args == null)
            {
                return null;
            }

            var builder = new StringBuilder();
            foreach (string arg in Args)
            {
                if (builder.Length > 0)
                    builder.Append(" ");
                
                builder.Append("\"")
                    .Append(arg.Replace("\\", "\\\\").Replace("\"", "\\\""))
                    .Append("\"");
            }
            return builder.ToString();
        }
    }
}