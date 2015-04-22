using System;

namespace Logger
{
    public interface ILogger
    {
        void Info(string msg, params Object[] args);

        void Error(string msg, params Object[] args);
    }
}
