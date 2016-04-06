using System;
using System.Threading;

namespace PoC.RxExamples.Logging
{
    /// <summary>
    /// A logger that writes to Console.Out.
    /// </summary>
    public class ConsoleLog : ILog, ILog<ConsoleLog>
    {
        private string _loggerName;

        void Write(string level, string message, Exception exception = null)
        {
            var threadName = Thread.CurrentThread.Name;
            Console.Out.WriteLine(
                "{0} [{1}] '{2}' {3}",
                level, threadName, _loggerName, message);

            if (exception == null)
                return;

            Console.Out.WriteLine(
                "{0} [{1}] '{2}' {3}: {4} {5}", 
                level, threadName, _loggerName, exception.GetType().FullName, exception.Message, exception.StackTrace);
        }

        public void InitializeFor(string loggerName)
        {
            _loggerName = loggerName;
        }

        public void Debug(string message, params object[] formatting)
        {
            Write("DEBUG", String.Format(message, formatting));
        }

        public void Debug(Func<string> message)
        {
            Write("DEBUG", message());
        }

        public void Info(string message, params object[] formatting)
        {
            Write("INFO", String.Format(message, formatting));
        }

        public void Info(Func<string> message)
        {
            Write("INFO", message());
        }

        public void Warn(string message, params object[] formatting)
        {
            Write("WARN", String.Format(message, formatting));
        }

        public void Warn(Func<string> message)
        {
            Write("WARN", message());
        }

        public void Error(string message, params object[] formatting)
        {
            Write("ERROR", String.Format(message, formatting));
        }

        public void Error(Func<string> message)
        {
            Write("ERROR", message());
        }

        public void Error(Func<string> message, Exception exception)
        {
            Write("ERROR", message(), exception);
        }

        public void Fatal(string message, params object[] formatting)
        {
            Write("FATAL", String.Format(message, formatting));
        }

        public void Fatal(Func<string> message)
        {
            Write("FATAL", message());
        }

        public void Fatal(Func<string> message, Exception exception)
        {
            Write("FATAL", message(), exception);
        }
    }
}