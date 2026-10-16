using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using NLog;
using System.IO.Ports;
using System.IO;

namespace MastercardHost
{
    public partial class MainForm : Form, INotifyPropertyChanged
    {
        private MainViewModel _viewModel;
        private int _logLimit;
        private TestForm _testForm;
        private bool _logRefreshScheduled;

        public MainForm()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindViewModel();
            TranslateLanguage();
            InitLogLimitStatus();
        }

        public MainViewModel MainViewModel
        {
            get => _viewModel;
        }

        public int LogLimit
        {
            get => _logLimit;
            set
            {
                if (_logLimit != value)
                {
                    _logLimit = value;
                    OnPropertyChanged(nameof(LogLimit));
                }
            }
        }

        private void BindViewModel()
        {
            try
            {
                //绑定IP地址、端口输入框属性
                comboBox_IPAddr.DataBindings.Add("Text", _viewModel, nameof(_viewModel.ServerIPAddr), true, DataSourceUpdateMode.OnPropertyChanged);
                Binding portServerBinding = new Binding("Text", _viewModel, nameof(_viewModel.ServerPort), true, DataSourceUpdateMode.OnPropertyChanged);
                portServerBinding.Format += (sender, e) =>
                {
                    if (e.DesiredType != typeof(string))
                    {
                        return;
                    }
                    e.Value = e.Value?.ToString() ?? string.Empty;
                };

                portServerBinding.Parse += (sender, e) =>
                {
                    if (e.DesiredType != typeof(int))
                    {
                        return;
                    }
                    if (int.TryParse((string)e.Value, out int result))
                    {
                        e.Value = result;
                    }
                    else
                    {
                        e.Value = 0;
                    }
                };
                textBox_Port_Server.DataBindings.Add(portServerBinding);

                textBox_RespCode.DataBindings.Add("Text", _viewModel, nameof(_viewModel.RespCode), false, DataSourceUpdateMode.OnPropertyChanged);
                textBox_IAD.DataBindings.Add("Text", _viewModel, nameof(_viewModel.IAD), false, DataSourceUpdateMode.OnPropertyChanged);
                textBox_Script.DataBindings.Add("Text", _viewModel, nameof(_viewModel.Script), false, DataSourceUpdateMode.OnPropertyChanged);

                //禁用输入功能
                richTextBox1.ReadOnly = true;
                // 日志区刷新：把同一批次内的多次变更合并为一次重建，
                // 避免每条日志都重建整个文本区（原实现在日志量大时是 O(n²)）
                _viewModel.Logs.CollectionChanged += (sender, e) => ScheduleLogRefresh();

                //绑定按钮Enabled属性
                button_Listen_Server.Click += (sender, e) => _viewModel.startListenCommand.Execute(null);
                Binding bindButtonServerStart = new Binding("Enabled", _viewModel, nameof(_viewModel.IsListenEnabled), true, DataSourceUpdateMode.OnPropertyChanged);
                button_Listen_Server.DataBindings.Add(bindButtonServerStart);

                button_Close_Server.Click += (sender, e) => _viewModel.stopListenCommand.Execute(null);
                Binding bindButtonServerStop = new Binding("Enabled", _viewModel, nameof(_viewModel.IsStopListenEnabled), true, DataSourceUpdateMode.OnPropertyChanged);
                button_Close_Server.DataBindings.Add(bindButtonServerStop);


                button_OpenSerial.Click += (sender, e) => _viewModel.OpenSerialPortCommand.Execute(null);
                Binding bindButtonSerialOpen = new Binding("Enabled", _viewModel, nameof(_viewModel.IsOpenSerialEnabled), true, DataSourceUpdateMode.OnPropertyChanged);
                button_OpenSerial.DataBindings.Add(bindButtonSerialOpen);

                button_CloseSerial.Click += (sender, e) => _viewModel.CloseSerialPortCommand.Execute(null);
                Binding bindButtonSerialClose = new Binding("Enabled", _viewModel, nameof(_viewModel.IsCloseSerialEnabled), true, DataSourceUpdateMode.OnPropertyChanged);
                button_CloseSerial.DataBindings.Add(bindButtonSerialClose);

                button_ClearScreen.Click += (sender, e) => _viewModel.clearScreenCommand.Execute(null);

                Binding bindLogLimit = new Binding(nameof(LogLimit), _viewModel, nameof(_viewModel.MaxLogCount), true, DataSourceUpdateMode.OnPropertyChanged);
                this.DataBindings.Add(bindLogLimit);
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"BindViewModel Exception: {ex}");
            }
        }

        private void TranslateLanguage()
        {
            this.Text = Properties.Resources.Title;
            this.label_Config_Info.Text = Properties.Resources.Config;
            this.label_CAPK_Info.Text = Properties.Resources.CAPK;
            this.label_Revokey.Text = Properties.Resources.Revokey;
            this.label_RespCode.Text = Properties.Resources.RespCode;
            this.label_IssuerAuthData.Text = Properties.Resources.IssuerAuthData;
            this.label_Script.Text = Properties.Resources.Script;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 触发日志区刷新。同一批次内的多次变更只投递一次，避免重复重建文本。
        /// </summary>
        private void ScheduleLogRefresh()
        {
            if (_logRefreshScheduled)
            {
                return;
            }

            _logRefreshScheduled = true;

            if (richTextBox1.IsHandleCreated)
            {
                richTextBox1.BeginInvoke(new Action(RefreshLogText));
            }
            else
            {
                RefreshLogText();
            }
        }

        private void RefreshLogText()
        {
            _logRefreshScheduled = false;
            richTextBox1.Text = string.Join(Environment.NewLine, _viewModel.Logs);
            richTextBox1.ScrollToCaret();
        }

        /// <summary>
        /// 按当前 LogLimit 同步"日志区行数"菜单的勾选状态。
        /// </summary>
        private void SyncLogLimitMenuChecks()
        {
            this.toolStripMenuItem3.Checked = (this.LogLimit == 20);
            this.toolStripMenuItem4.Checked = (this.LogLimit == 50);
            this.toolStripMenuItem5.Checked = (this.LogLimit == 100);
            this.toolStripMenuItem6.Checked = (this.LogLimit == 200);
            this.ToolStripMenuItem_SelfDefine.Checked = !this.toolStripMenuItem3.Checked
                                                    && !this.toolStripMenuItem4.Checked
                                                    && !this.toolStripMenuItem5.Checked
                                                    && !this.toolStripMenuItem6.Checked;
        }

        /// <summary>
        /// 设置日志区保留行数上限。LogLimit 经数据绑定同步到 ViewModel.MaxLogCount。
        /// </summary>
        private void SetLogLimit(int limit)
        {
            if (limit <= 0)
            {
                return;
            }

            this.LogLimit = limit;
            SyncLogLimitMenuChecks();
            MyLogManager.Log($"日志区行数上限设置为 {limit}");
        }

        private void InitLogLimitStatus()
        {
            // 按 ViewModel 当前的 MaxLogCount 同步勾选状态（默认值 1000 不在预设项中）
            SyncLogLimitMenuChecks();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            SetLogLimit(20);
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            SetLogLimit(50);
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            SetLogLimit(100);
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            SetLogLimit(200);
        }

        private void ToolStripMenuItem_SelfDefine_Click(object sender, EventArgs e)
        {
            try
            {
                //弹出输入框
                using (FormDialogBox dialogBox = new FormDialogBox())
                {
                    if (dialogBox.ShowDialog() == DialogResult.OK)
                    {
                        string input = dialogBox.InputResult;
                        if (int.TryParse(input, out int customLimit) && customLimit > 0)
                        {
                            SetLogLimit(customLimit);
                        }
                        else
                        {
                            // 显示弹窗提示用户
                            System.Windows.MessageBox.Show("Input not valid number", "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
                            this.ToolStripMenuItem_SelfDefine.Checked = false;
                        }
                    }
                    else
                    {
                        this.ToolStripMenuItem_SelfDefine.Checked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Exception: {ex}");
            }
        }

        private void button_Test_Click(object sender, EventArgs e)
        {
            if (_testForm == null || _testForm.IsDisposed)
            {
                _testForm = new TestForm(this);
                _testForm.Closed += (s, args) => _testForm = null; // 确保引用被清除
                _testForm.Show();
                // _viewModel.OnLoopACTSend += (acitonName) =>
                // {
                //     _testForm.HandleLoopACTActionRequest(acitonName);
                // };
            }
            else
            {
                _testForm.Focus(); // 如果窗口已打开，将焦点移至该窗口
            }
        }

        private void comboBox_SerialPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            //获取当前选中的串口号，设置到ViewModel中
            try
            {
                _viewModel.SelectedPortName = comboBox_SerialPort.SelectedItem.ToString();
                _viewModel.UpdateLogText(_viewModel.SelectedPortName + " selected");
                MyLogManager.Log(_viewModel.SelectedPortName + "已选中");
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"选择串口异常:{ex}");
            }
        }

        private void comboBox_SerialPort_DropDown(object sender, EventArgs e)
        {
            //获取所有可用串口号
            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                System.Windows.MessageBox.Show("No serial port available", "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
                MyLogManager.Log("No serial port available");
                return;
            }
            //把串口号添加到下拉框
            comboBox_SerialPort.Items.Clear();
            foreach (string port in ports)
            {
                comboBox_SerialPort.Items.Add(port);
            }
        }

        private void comboBox_IPAddr_DropDown(object sender, EventArgs e)
        {
            //获取本机所有可用的IPv4地址
            comboBox_IPAddr.Items.Clear();
            foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    comboBox_IPAddr.Items.Add(ip.ToString());
                }
            }
            //如果没有可用的IPv4地址，弹出提示框
            if (comboBox_IPAddr.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("No IPv4 address available", "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
                MyLogManager.Log("No IPv4 address available");
            }
        }

        private void comboBox_IPAddr_SelectedIndexChanged(object sender, EventArgs e)
        {
            //获取当前选中的IP地址，设置到ViewModel中
            _viewModel.ServerIPAddr = comboBox_IPAddr.SelectedItem.ToString();
            _viewModel.UpdateLogText(_viewModel.ServerIPAddr + "已选中");
            MyLogManager.Log(_viewModel.ServerIPAddr + "已选中");
        }

        private void comboBox_Config_DropDown(object sender, EventArgs e)
        {
            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            string configDir = runDir + "Config\\Config\\";
            MyLogManager.Log($"Config Dir:{configDir}");

            if (Directory.Exists(configDir))
            {
                string[] files = Directory.GetFiles(configDir);
                if (files.Length == 0)
                {
                    System.Windows.MessageBox.Show("No config file available", "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
                    MyLogManager.Log("No config file available");
                    return;
                }
                comboBox_Config.Items.Clear();
                foreach (string file in files)
                {
                    //去掉文件的后缀名，只显示文件名
                    string fileName = file.Substring(file.LastIndexOf("\\") + 1);
                    fileName = fileName.Substring(0, fileName.LastIndexOf("."));
                    comboBox_Config.Items.Add(fileName);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No config file available", "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
                MyLogManager.Log("No config file available");

            }
        }

        private void comboBox_Config_SelectedIndexChanged(object sender, EventArgs e)
        {
            _viewModel.SelectConfig = comboBox_Config.SelectedItem.ToString();
            _viewModel.UpdateLogText("Download config " + _viewModel.SelectConfig);
            MyLogManager.Log("下载配置" + _viewModel.ServerIPAddr);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"释放 ViewModel 失败: {ex}");
            }

            base.OnFormClosed(e);
        }
    }
}
