using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace RobotFramework.Core
{
    /// <summary>
    /// Log level enumeration - similar to ROS log levels
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// Log message structure
    /// </summary>
    public class LogMessage
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string NodeName { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public string Function { get; set; }
        public int Line { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{NodeName}] {Message}";
        }
    }

    /// <summary>
    /// Logger class - similar to ROS logging system
    /// </summary>
    public class Logger
    {
        private static readonly Lazy<Logger> _instance = new(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly ConcurrentQueue<LogMessage> _logQueue;
        private readonly string _logDirectory;
        private readonly object _fileLock = new object();
        private LogLevel _minLogLevel = LogLevel.Info;

        public event Action<LogMessage> LogReceived;

        private Logger()
        {
            _logQueue = new ConcurrentQueue<LogMessage>();
            _logDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
            Directory.CreateDirectory(_logDirectory);

            // Start log writing task
            Task.Run(ProcessLogQueue);
        }

        /// <summary>
        /// Set minimum log level
        /// </summary>
        public void SetLogLevel(LogLevel level)
        {
            _minLogLevel = level;
            Console.WriteLine($"[Logger] Log level set to: {level}");
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public void Debug(string nodeName, string message, 
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string function = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Debug, nodeName, message, file, function, line);
        }

        /// <summary>
        /// Log info message
        /// </summary>
        public void Info(string nodeName, string message,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string function = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Info, nodeName, message, file, function, line);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public void Warn(string nodeName, string message,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string function = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Warn, nodeName, message, file, function, line);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public void Error(string nodeName, string message,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string function = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Error, nodeName, message, file, function, line);
        }

        /// <summary>
        /// Log fatal error message
        /// </summary>
        public void Fatal(string nodeName, string message,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string function = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
        {
            Log(LogLevel.Fatal, nodeName, message, file, function, line);
        }

        /// <summary>
        /// Core logging method
        /// </summary>
        private void Log(LogLevel level, string nodeName, string message, string file, string function, int line)
        {
            if (level < _minLogLevel)
                return;

            var logMessage = new LogMessage
            {
                Timestamp = DateTime.Now,
                Level = level,
                NodeName = nodeName,
                Message = message,
                File = Path.GetFileName(file),
                Function = function,
                Line = line
            };

            _logQueue.Enqueue(logMessage);
            LogReceived?.Invoke(logMessage);

            // Output to console simultaneously
            var color = GetConsoleColor(level);
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(logMessage.ToString());
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Get console color for log level
        /// </summary>
        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Process log queue
        /// </summary>
        private async Task ProcessLogQueue()
        {
            while (true)
            {
                try
                {
                    if (_logQueue.TryDequeue(out var logMessage))
                    {
                        await WriteLogToFile(logMessage);
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Logger] Failed to write to log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Write log message to file
        /// </summary>
        private async Task WriteLogToFile(LogMessage logMessage)
        {
            var fileName = $"robot_{DateTime.Now:yyyy-MM-dd}.log";
            var filePath = Path.Combine(_logDirectory, fileName);

            lock (_fileLock)
            {
                File.AppendAllText(filePath, logMessage.ToString() + Environment.NewLine);
            }
        }
    }

    /// <summary>
    /// Extension methods for logging
    /// </summary>
    public static class LoggerExtensions
    {
        public static void LogDebug(this BaseNode node, string message) =>
            Logger.Instance.Debug(node.Name, message);

        public static void LogInfo(this BaseNode node, string message) =>
            Logger.Instance.Info(node.Name, message);

        public static void LogWarn(this BaseNode node, string message) =>
            Logger.Instance.Warn(node.Name, message);

        public static void LogError(this BaseNode node, string message) =>
            Logger.Instance.Error(node.Name, message);

        public static void LogFatal(this BaseNode node, string message) =>
            Logger.Instance.Fatal(node.Name, message);
    }
}