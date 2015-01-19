using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Containerizer.Models
{
    public static class ArgumentEscaper
    {
        public static string Escape(string[] args)
        {
            var builder = new StringBuilder();
            foreach (string arg in args)
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