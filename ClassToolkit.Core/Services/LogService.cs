using ClassToolkit.Core.Utilities;
using System.Text;

namespace ClassToolkit.Core.Services
{

    public static class LogService
    {
        // 路径初始化
        static string logDir = DataPathHelper.GetDataPath("log");
        static string logFile = Path.Combine(logDir, "running.log");
        private static string? _who;

        // 创建安全锁
        private static readonly object _fileLock = new object();

        static LogService()
        {
            Directory.CreateDirectory(logDir);
        }

        public static void Init(string who)
        {
            _who = who;
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
        }

        /// <summary>
        /// 完整日志实现
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="message">消息</param>
        public static void Log(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("[yyyy-MM-dd][HH:mm:ss]");  // 时间格式
            string logFormat = $"{timestamp}[{level}][{_who}]: {message}";               // 日志格式

            // 线程锁,安全
            lock (_fileLock)
            {
                File.AppendAllText(logFile, logFormat + Environment.NewLine, Encoding.UTF8);
            }

        }

        // 便捷使用
        public static void Info(string message) => Log("INFO", message);
        public static void Warn(string message) => Log("WARN", message);
        public static void Error(string message) => Log("ERROR", message);

    }
}
