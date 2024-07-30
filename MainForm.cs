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

namespace MastercardHost
{
    public partial class MainForm : Form
    {
        private MainViewModel _viewModel;
        public MainForm()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindViewModel();
            MyLogManager.Log($"_isListenEnabled = {_viewModel.IsListenEnabled}");
            MyLogManager.Log($"_isStopListenEnabled = {_viewModel.IsStopListenEnabled}");
            MyLogManager.Log($"_isBindEnabled = {_viewModel.IsBindEnabled}");
            MyLogManager.Log($"_isStopBindEnabled = {_viewModel.IsStopBindEnabled}");
        }

        private void BindViewModel()
        {
            //绑定IP地址、端口输入框属性
            textBox_IP_Addr_Server.DataBindings.Add("Text", _viewModel, nameof(_viewModel.ServerIPAddr), false, DataSourceUpdateMode.OnPropertyChanged);
            var portServerBinding = new Binding("Text", _viewModel, nameof(_viewModel.ServerPort), true, DataSourceUpdateMode.OnPropertyChanged);
            portServerBinding.Format += (sender , e) =>
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

            textBox_IP_Addr_Client.DataBindings.Add("Text", _viewModel, nameof(_viewModel.ClientIPAddr), false, DataSourceUpdateMode.OnPropertyChanged);
            var portClientBinding = new Binding("Text", _viewModel, nameof(_viewModel.ClientPort), true, DataSourceUpdateMode.OnPropertyChanged);
            portClientBinding.Format += (sender, e) =>
            {
                if (e.DesiredType != typeof(string))
                {
                    return;
                }
                e.Value = e.Value?.ToString() ?? string.Empty;
            };
            portClientBinding.Parse += (sender, e) =>
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
            textBox_Port_Client.DataBindings.Add(portClientBinding);

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

            button_Bind.Click += (sender, e) => _viewModel.startBindCommand.Execute(null);
            Binding bindButtonClientStart = new Binding("Enabled", _viewModel, nameof(_viewModel.IsBindEnabled), true, DataSourceUpdateMode.OnPropertyChanged);
            button_Bind.DataBindings.Add(bindButtonClientStart);

            button_Close_Client.Click += (sender, e) => _viewModel.stopBindCommand.Execute(null);
            Binding bindBuutonClientStop = new Binding("Enabled", _viewModel, nameof(_viewModel.IsStopBindEnabled), true, DataSourceUpdateMode.OnPropertyChanged);
            button_Close_Client.DataBindings.Add(bindBuutonClientStop);

            button_ClearScreen.Click += (sender, e) => _viewModel.clearScreenCommand.Execute(null);

            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.IsListenEnabled))
                {
                    MyLogManager.Log($"PropertyChanged: IsListenEnabled = {_viewModel.IsListenEnabled}");
                    button_Listen_Server.Enabled = _viewModel.IsListenEnabled;
                }
                if (e.PropertyName == nameof(_viewModel.IsStopListenEnabled))
                {
                    MyLogManager.Log($"PropertyChanged: IsStopListenEnabled = {_viewModel.IsStopListenEnabled}");
                    button_Close_Server.Enabled = _viewModel.IsStopListenEnabled;
                }
            };
        }

        private void SetLanguage(string cultureCode)
        {
            CultureInfo culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }

        private void UpdateLanguage()
        {
            // 更新控件的文本
            this.Text = Properties.Resources.Title;
            this.button_ClearScreen.Text = Properties.Resources.ClearScreen;
        }

        private void TranslateLanguage()
        {

        }

    }
}
