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

        public MainForm()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindViewModel();
            TranslateLanguage();
            InitLogLimitStatus();
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
            //绑定IP地址、端口输入框属性
            comboBox_IPAddr.DataBindings.Add("Text", _viewModel, nameof(_viewModel.ServerIPAddr), true, DataSourceUpdateMode.OnPropertyChanged);
            var portServerBinding = new Binding("Text", _viewModel, nameof(_viewModel.ServerPort), true, DataSourceUpdateMode.OnPropertyChanged);
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
            //richTextBox1.DataBindings.Add("Text", _viewModel, nameof(_viewModel.MainModel), false, DataSourceUpdateMode.OnPropertyChanged);
            _viewModel.OutcomeText.CollectionChanged += (sender, e) =>
            {
                richTextBox1.Invoke((MethodInvoker)(() =>
                {
                    richTextBox1.Text = string.Join(Environment.NewLine, _viewModel.OutcomeText);
                }));
            };

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

            Binding bindLogLimit = new Binding(nameof(LogLimit), _viewModel, nameof(_viewModel.OutcomeLimit), true, DataSourceUpdateMode.OnPropertyChanged);
            this.DataBindings.Add(bindLogLimit);
        }

        private void Button_Close_Server_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
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

        private void InitLogLimitStatus()
        {
            this.toolStripMenuItem3.Checked = false;
            this.toolStripMenuItem4.Checked = false;
            this.toolStripMenuItem5.Checked = true;
            this.toolStripMenuItem6.Checked = false;
            this.ToolStripMenuItem_SelfDefine.Checked = false;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            this.toolStripMenuItem3.Checked = !this.toolStripMenuItem3.Checked;

            this.toolStripMenuItem4.Checked = false;
            this.toolStripMenuItem5.Checked = false;
            this.toolStripMenuItem6.Checked = false;
            this.ToolStripMenuItem_SelfDefine.Checked = false;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            this.toolStripMenuItem4.Checked = !this.toolStripMenuItem4.Checked;

            this.toolStripMenuItem3.Checked = false;
            this.toolStripMenuItem5.Checked = false;
            this.toolStripMenuItem6.Checked = false;
            this.ToolStripMenuItem_SelfDefine.Checked = false;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            this.toolStripMenuItem5.Checked = !this.toolStripMenuItem5.Checked;

            this.toolStripMenuItem3.Checked = false;
            this.toolStripMenuItem4.Checked = false;
            this.toolStripMenuItem6.Checked = false;
            this.ToolStripMenuItem_SelfDefine.Checked = false;
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            this.toolStripMenuItem6.Checked = !this.toolStripMenuItem6.Checked;

            this.toolStripMenuItem3.Checked = false;
            this.toolStripMenuItem4.Checked = false;
            this.toolStripMenuItem5.Checked = false;
            this.ToolStripMenuItem_SelfDefine.Checked = false;
        }

        private void ToolStripMenuItem_SelfDefine_Click(object sender, EventArgs e)
        {
            try
            {
                this.ToolStripMenuItem_SelfDefine.Checked = !this.ToolStripMenuItem_SelfDefine.Checked;

                //如果点击之后是选中状态再弹出输入框
                if (this.ToolStripMenuItem_SelfDefine.Checked)
                {
                    //弹出输入框
                    using (FormDialogBox dialogBox = new FormDialogBox())
                    {
                        if (dialogBox.ShowDialog() == DialogResult.OK)
                        {
                            string input = dialogBox.InputResult;
                            if (input == null || input.Length == 0)
                            {
                                this.ToolStripMenuItem_SelfDefine.Checked = false;
                            }
                            else
                            {
                                if (int.TryParse(input, out _logLimit))
                                {

                                }
                                else
                                {
                                    // 显示弹窗提示用户
                                    System.Windows.MessageBox.Show("Input not valid number", "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
                                }
                            }
                        }
                        else
                        {
                            this.ToolStripMenuItem_SelfDefine.Checked = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_Test_Click(object sender, EventArgs e)
        {
            if (_testForm == null || _testForm.IsDisposed)
            {
                _testForm = new TestForm(this);
                _testForm.Closed += (s, args) => _testForm = null; // 确保引用被清除
                _testForm.Show();
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
                richTextBox1.Text += richTextBox1.Text + _viewModel.SelectedPortName + "已选中" + Environment.NewLine;
                MyLogManager.Log(_viewModel.SelectedPortName + "已选中");
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"选择串口异常:{ex.Message}");
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
            richTextBox1.Text += richTextBox1.Text + _viewModel.ServerIPAddr + "已选中" + Environment.NewLine;
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
            richTextBox1.Text += richTextBox1.Text + "下载配置" + _viewModel.SelectConfig + Environment.NewLine;
            MyLogManager.Log("下载配置" + _viewModel.ServerIPAddr);
        }
    }
}
