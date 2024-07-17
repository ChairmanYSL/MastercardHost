using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace MastercardHost
{
    public static class MyLogManager
    {
        private static readonly Logger ConsoleLogger = LogManager.GetLogger("logconsole");
        private static readonly Logger FileLogger = LogManager.GetLogger("logfile");
        private static readonly Logger DefaultLogger = LogManager.GetCurrentClassLogger();

        public static void Log(string message, string target)
        {
            switch (target.ToLower())
            {
                case "console":
                    ConsoleLogger.Info(message);
                    break;
                case "file":
                    FileLogger.Info(message);
                    break;
                case "both":
                default:
                    DefaultLogger.Info(message); // 默认Logger写入所有目标
                    break;
            }
        }

        public static void Log(string message)
        {
            DefaultLogger.Info(message);
        }
    }
}
