using System;

namespace Logger
{
    public class Logger : ILogger
    {
        private string component;

        public Logger(string component)
        {
            this.component = component;
        }

        public void Info(string msg, params Object[] args)
        {
            if (args.Length > 0)
            {
                Console.WriteLine(msg, args);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        public void Error(string msg, params Object[] args)
        {
            if (args.Length > 0)
            {
                Console.Error.WriteLine(msg, args);
            }
            else
            {
                Console.Error.WriteLine(msg);
            }
        }
    }
}
