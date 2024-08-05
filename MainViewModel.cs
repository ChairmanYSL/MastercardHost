using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MvvmHelpers;
using MvvmHelpers.Commands;
using System.Windows.Input;
using System.Xml.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TcpSharp;
using System.Net;
using System.Windows;
using System.Windows.Forms;


namespace MastercardHost
{
    public class MainViewModel : BaseViewModel
    {
        public Command startListenCommand { get; }
        public Command stopListenCommand { get; }
        public Command startBindCommand { get; }
        public Command stopBindCommand { get; }
        public Command clearScreenCommand { get; }

        //private TcpSharpSocketClient _tcpClient;
        //private TcpSharpSocketServer _tcpServer;

        private readonly DataProcessor _dataProcessor;
        private readonly TcpCommunication tcpCommunication;

        private bool _isListenEnabled;
        private bool _isStopListenEnabled;
        private bool _isBindEnabled;
        private bool _isStopBindEnabled;

        private int _server_port;
        private string _server_ipAddr;
        private int _client_port;
        private string _client_ipAddr;

        private string _respCode;
        private string _iad;
        private string _script;
        private ObservableRangeCollection<string> _outcomeText;

        private int _outcomeLimit;

        public MainViewModel()
        {
            tcpCommunication = new TcpCommunication();
            _dataProcessor = new DataProcessor();

            tcpCommunication.OnClientDataReceived += _dataProcessor.Client_OnDataReceived;
            tcpCommunication.OnServerDataReceived += _dataProcessor.Server_OnDataReceived;

            _dataProcessor.outcomeNeeded += UpdateLogText;

            startListenCommand = new Command(StartListen);
            stopListenCommand = new Command(StopListen);
            startBindCommand = new Command(StartBind);
            stopBindCommand = new Command (StopBind);
            clearScreenCommand = new Command(CleanScreen);

            _isListenEnabled = true;
            _isBindEnabled = true;
            _isStopListenEnabled = false;
            _isStopBindEnabled = false;

            _server_port = 8765;
            _server_ipAddr = "127.0.0.1";
            _client_port = 8766;
            _client_ipAddr = "127.0.0.1";

            _respCode = "00";
            _iad = "";
            _script = "";
            _outcomeText = new ObservableRangeCollection<string>();
        }

        public bool IsListenEnabled
        {
            get => _isListenEnabled;
            set =>  SetProperty(ref _isListenEnabled, value);
        }

        public bool IsStopListenEnabled
        {
            get => _isStopListenEnabled;
            set => SetProperty(ref _isStopListenEnabled, value);
        }

        public bool IsBindEnabled
        {
            get => _isBindEnabled;
            set => SetProperty(ref _isBindEnabled, value);
        }

        public bool IsStopBindEnabled
        {
            get => _isStopBindEnabled;
            set => SetProperty(ref _isStopBindEnabled, value);
        }

        public int ServerPort
        {
            get => _server_port;
            set => SetProperty(ref _server_port, value);
        }

        public string ServerIPAddr
        {
            get => _server_ipAddr;
            set => SetProperty(ref _server_ipAddr, value);
        }

        public int ClientPort
        {
            get => _client_port;
            set => SetProperty(ref _client_port, value);
        }

        public string ClientIPAddr
        {
            get => _client_ipAddr;
            set => SetProperty (ref _client_ipAddr, value);
        }

        public string RespCode
        {
            get => _respCode;
            set => SetProperty(ref _respCode, value);
        }

        public string IAD
        {
            get => _iad;
            set => SetProperty(ref _iad, value);
        }

        public string Script
        {
            get => _script;
            set => SetProperty(ref _script, value);
        }

        public ObservableRangeCollection<string> OutcomeText
        {
            get => _outcomeText;
            set => SetProperty(ref _outcomeText, value);
        }

        public int OutcomeLimit
        {
            get => _outcomeLimit;
            set => SetProperty(ref _outcomeLimit, value);
        }

        public void UpdateLogText(string text)
        {
            _outcomeText.Add(text+Environment.NewLine);
            //TODO: 添加通过菜单控制自动清log的行数限制
            if (_outcomeText.Count > 100)
            {
                _outcomeText.RemoveAt(0);
            }
        }

        private void StartListen(object parameter)
        {
            try
            {
                tcpCommunication.StartServer(_server_port, UpdateLogText);
                IsStopListenEnabled = true;
                IsListenEnabled = false;
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private void StopListen(object parameter)
        {
            try
            {
                tcpCommunication.StopServer();
                IsStopListenEnabled= false;
                IsListenEnabled = true;
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private void StartBind(object parameter)
        {
            try
            {
                tcpCommunication.ConnectClient(_client_ipAddr, _client_port, UpdateLogText);
                IsBindEnabled = false;
                IsStopBindEnabled = true;
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private void StopBind(object parameter)
        {
            try
            {
                tcpCommunication.DisconnectClient();
                IsStopBindEnabled = false;
                IsBindEnabled= true;
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private void CleanScreen(object parameter)
        {
            _outcomeText.RemoveAt(0);
        }

    }
}
