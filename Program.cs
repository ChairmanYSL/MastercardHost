using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MastercardHost
{

    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            var currentCulture = CultureInfo.CurrentUICulture;

            // 配置NLog
            MyLogManager.Log("Application started");
            MyLogManager.Log($"当前区域性:{currentCulture.Name}");
            MyLogManager.Log($"区域性名称:{currentCulture.DisplayName}");

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

            MyLogManager.Log($"当前线程的文化信息:{Thread.CurrentThread.CurrentUICulture}");


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
