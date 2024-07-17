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
            _viewModel.MainModel = new MainModel()
            {
                ServerSettings = new ServerSettings()
                {
                    IpAddress = string.Empty,
                    Port = 6908,
                },
                ClientSettings = new ClientSettings()
                {
                    IpAddress = string.Empty,
                    Port = 6909,
                },
                Config = new Config()
                {
                    _aidConfig = string.Empty,
                    _capkConfig = string.Empty, 
                    _revopkConfig = string.Empty,
                },
                RespCode = "00",
                Iad = "",
                Script = "",
                OutcomeText = new MvvmHelpers.ObservableRangeCollection<string>()
            };
            BindViewModel();
        }

        private void BindViewModel()
        {
            textBox_IP_Addr_Server.DataBindings.Add("Text", _viewModel.MainModel.ServerSettings, "IpAddress", false, DataSourceUpdateMode.OnPropertyChanged);
            var portServerBinding = new Binding("Text", _viewModel.MainModel.ServerSettings, "Port", true, DataSourceUpdateMode.OnPropertyChanged);
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

            textBox_IP_Addr_Client.DataBindings.Add("Text", _viewModel.MainModel.ClientSettings, "IpAddress", false, DataSourceUpdateMode.OnPropertyChanged);
            var portClientBinding = new Binding("Text", _viewModel.MainModel.ClientSettings, "Port", true, DataSourceUpdateMode.OnPropertyChanged);
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

            textBox_RespCode.DataBindings.Add("Text", _viewModel.MainModel, "RespCode", false, DataSourceUpdateMode.OnPropertyChanged);
            textBox_IAD.DataBindings.Add("Text", _viewModel.MainModel, "Iad", false, DataSourceUpdateMode.OnPropertyChanged);
            textBox_Script.DataBindings.Add("Text", _viewModel.MainModel, "Script", false, DataSourceUpdateMode.OnPropertyChanged);
            richTextBox1.DataBindings.Add("Text", _viewModel.MainModel, "OutcomeText", false, DataSourceUpdateMode.OnPropertyChanged);

            button_Listen_Server.Click += (sender, e) => _viewModel.startListenCommand.Execute(null);
            _viewModel.startListenCommand.CanExecuteChanged += (sender, e) => { 
                button_Listen_Server.Enabled = _viewModel.startListenCommand.CanExecute(null); 
            };

            button_Close_Server.Click += (sender, e) => _viewModel.stopListenCommand.Execute(null);
            _viewModel.stopListenCommand.CanExecuteChanged += (sender, e) =>
            {
                button_Close_Server.Enabled = _viewModel.stopListenCommand.CanExecute(null);
            };

            button_Bind.Click += (sender, e) => _viewModel.startBindCommand.Execute(null);
            _viewModel.startBindCommand.CanExecuteChanged += (sender, e) =>
            {
                button_Bind.Enabled = _viewModel.startBindCommand.CanExecute(null);
            };

            button_Close_Client.Click += (sender, e) => _viewModel.stopBindCommand.Execute(null);
            _viewModel.startBindCommand.CanExecuteChanged += (sender, e) =>
            {
                button_Close_Client.Enabled = _viewModel.stopBindCommand.CanExecute(null);
            };


            button_ClearScreen.Click += (sender, e) => _viewModel.clearScreenCommand.Execute(null);
        }

        private void TranslateLanguage()
        {

        }

    }
}
