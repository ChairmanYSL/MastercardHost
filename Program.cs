using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog.Config;
using System.IO;

namespace MastercardHost
{
    internal static class Program
    {
        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool AllocConsole();
        private static readonly Logger Logger = LogManager.GetLogger("logconsole");
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //AllocConsole();
            //Console.WriteLine("Hello, World!");
            var currentCulture = CultureInfo.CurrentUICulture;

            // 配置NLog
            //MyLogManager.Log("Application started");
            //MyLogManager.Log($"当前区域性:{currentCulture.Name}");
            //MyLogManager.Log($"区域性名称:{currentCulture.DisplayName}");

            // 检查是否为中文（包括所有中文区域性，例如简体中文、繁体中文等）
            if (currentCulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                // 设置当前线程的文化信息为中文（中国）
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
            }
            else
            {
                // 设置当前线程的文化信息为默认语言（英文）
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }

            //MyLogManager.Log($"当前线程的文化信息:{Thread.CurrentThread.CurrentUICulture}");

            //// 设置当前线程的文化信息为系统的UI文化
            //CultureInfo culture = CultureInfo.CurrentUICulture;
            //Thread.CurrentThread.CurrentUICulture = culture;
            //Thread.CurrentThread.CurrentCulture = culture;

            //MyLogManager.Log($"当前线程的文化信息:{Thread.CurrentThread.CurrentUICulture}");

            // 检查配置文件是否加载成功，并获取文件路径
            // 检查配置文件的来源
            string configFilePath = GetNLogConfigFilePath();
            if (!string.IsNullOrEmpty(configFilePath))
            {
                MyLogManager.Log($"NLog configuration file loaded from: {configFilePath}");
            }
            else
            {
                MessageBox.Show("NLog configuration file path could not be determined.", "NLog Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 测试日志输出
            Logger.Info("This is a test log to the console.");
            Logger.Debug("This debug message might not appear if minlevel is Info.");
            Logger.Error("This is a test error message.");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
        private static string GetNLogConfigFilePath()
        {
            // 尝试获取 NLog 配置文件路径
            if (LogManager.Configuration is XmlLoggingConfiguration xmlConfig)
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"); // 假设使用默认配置文件名
            }

            // 如果使用其他加载方式，可以通过内部日志查看加载的路径
            const string internalLogFile = "logs/nlog-internal.log";
            if (File.Exists(internalLogFile))
            {
                return internalLogFile;
            }

            return null; // 如果未找到路径
        }
    }
}
