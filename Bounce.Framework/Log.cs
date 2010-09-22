using System;
using System.IO;

namespace Bounce.Framework {
    class Log : ILog {
        private readonly TextWriter Stdout;
        private readonly TextWriter Stderr;
        private readonly LogOptions LogOptions;
        private readonly ILogMessageFormatter LogMessageFormatter;

        public Log(TextWriter stdout, TextWriter stderr, LogOptions logOptions, ILogMessageFormatter logMessageFormatter) {
            Stdout = stdout;
            Stderr = stderr;
            LogOptions = logOptions;
            this.LogMessageFormatter = logMessageFormatter;
        }

        public void Debug(string format, params object[] args) {
            WriteLogMessage(Stdout, LogLevel.Debug, String.Format(format, args));
        }

        public void Debug(object message) {
            WriteLogMessage(Stdout, LogLevel.Debug, message);
        }

        public ICommandLog BeginExecutingCommand(string command, string args) {
            if (LogOptions.CommandOutput) {
                return new CommandLog(command, args, Stdout, Stderr);
            } else {
                return new NullCommandLog();
            }
        }

        public void Info(string format, params object[] args) {
            WriteLogMessage(Stdout, LogLevel.Info, String.Format(format, args));
        }

        public void Info(object message) {
            WriteLogMessage(Stdout, LogLevel.Info, message);
        }

        public void Warning(string format, params object[] args) {
            WriteLogMessage(Stdout, LogLevel.Warning, String.Format(format, args));
        }

        public void Warning(object message) {
            WriteLogMessage(Stdout, LogLevel.Warning, message);
        }

        public void Warning(Exception exception, string format, params object[] args) {
            LogException(Stdout, LogLevel.Warning, String.Format(format, args), exception);
        }

        public void Warning(Exception exception, object message) {
            LogException(Stdout, LogLevel.Warning, message, exception);
        }

        public void Error(string format, params object[] args) {
            WriteLogMessage(Stderr, LogLevel.Error, String.Format(format, args));
        }

        public void Error(object message) {
            WriteLogMessage(Stderr, LogLevel.Error, message);
        }

        public void Error(Exception exception, string format, params object[] args) {
            LogException(Stderr, LogLevel.Error, String.Format(format, args), exception);
        }

        public void Error(Exception exception, object message) {
            LogException(Stderr, LogLevel.Error, message, exception);
        }

        private void WriteLogMessage(TextWriter output, LogLevel logLevel, object message) {
            if (logLevel <= LogOptions.LogLevel) {
                output.WriteLine(LogMessageFormatter.FormatLogMessage(DateTime.Now, logLevel, message));
            }
        }

        private void LogException(TextWriter output, LogLevel logLevel, object message, Exception exception) {
            WriteLogMessage(output, logLevel, message);
            WriteLogMessage(output, logLevel, exception);
        }
    }
}