using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void Log(string message, string target = "both")
        {
            var stackFrame = new StackTrace().GetFrame(1); // 获取调用Log方法的调用者信息
            var method = stackFrame.GetMethod();
            var callerFilePath = method.DeclaringType.FullName;
            var callerMemberName = method.Name;
            var callerLineNumber = stackFrame.GetFileLineNumber();
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Info, "", message);

            logEvent.Properties["CallerFilePath"] = callerFilePath;
            logEvent.Properties["CallerMemberName"] = callerMemberName;
            logEvent.Properties["CallerLineNumber"] = callerLineNumber;

            switch (target.ToLower())
            {
                case "console":
                    ConsoleLogger.Info(logEvent);
                    break;
                case "file":
                    FileLogger.Info(logEvent);
                    break;
                case "both":
                default:
                    DefaultLogger.Info(logEvent); // 默认Logger写入所有目标
                    break;
            }
        }

        //public static void Log(string message)
        //{
        //    LogEventInfo logEvent = new LogEventInfo(LogLevel.Info, "", message);

        //    logEvent.Properties["CallerFilePath"] = GetCallerFilePath();
        //    logEvent.Properties["CallerMemberName"] = GetCallerMemberName();
        //    logEvent.Properties["CallerLineNumber"] = GetCallerLineNumber();

        //    DefaultLogger.Info(message);
        //}
    }
}
