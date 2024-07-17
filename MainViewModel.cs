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
        private MainModel _mainModel;
        public Command startListenCommand { get; }
        public Command stopListenCommand { get; }
        public Command startBindCommand { get; }
        public Command stopBindCommand { get; }
        public Command clearScreenCommand { get; }

        private TcpSharpSocketClient _tcpClient;
        private TcpSharpSocketServer _tcpServer;

        private bool _isListenEnabled;
        private bool _isStopListenEnabled;
        private bool _isBindEnabled;
        private bool _isStopBindEnabled;


        public MainViewModel()
        {
            _mainModel = new MainModel();
            _tcpServer = new TcpSharpSocketServer();
            _tcpClient = new TcpSharpSocketClient();
            startListenCommand = new Command(StartListen);
            stopListenCommand = new Command(StopListen);
            startBindCommand = new Command(StartBind, CanStartBind);
            stopBindCommand = new Command (StopBind, CanStopBind);
            clearScreenCommand = new Command(CleanScreen);
            _isListenEnabled = true;
            _isBindEnabled = true;
        }

        public MainModel MainModel
        {
            get => _mainModel;
            set
            {
                if (_mainModel != value)
                {
                    _mainModel = value;
                    OnPropertyChanged(nameof(MainModel));
                }
            }
        }

        public void UpdateLogText(string text)
        {
            MainModel.OutcomeText.Add(text+Environment.NewLine);
            //TODO: 添加通过菜单控制自动清log的行数限制
            if (MainModel.OutcomeText.Count > 100)
            {
                MainModel.OutcomeText.RemoveAt(0);
            }
        }

        private void StartListen(object parameter)
        {
            try
            {
                if(_tcpServer == null)
                {
                    _tcpServer = new TcpSharpSocketServer();
                }

                if (_tcpServer.Listening)
                {
                    _tcpServer.StopListening();
                }

                _tcpServer.Port = _mainModel.ServerSettings.Port;
                _tcpServer.OnStarted += (sender, e) =>
                {
                    UpdateLogText($"Server Started Listen on {_tcpServer.Port}");
                };
                _tcpServer.OnError += (sender, e) =>
                {
                    UpdateLogText($"Server Error: {e.Exception.Message}");
                };
                _tcpServer.OnStopped += (sender, e) =>
                {
                    UpdateLogText("Server Stop Listen");
                };
                _tcpServer.OnConnected += (sender, e) =>
                {
                    UpdateLogText($"Server Connect on {e.IPAddress}:{e.Port}, Connect ID is: {e.ConnectionId}");
                };

                _tcpServer.StartListening();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private bool CanStartListen(object parameter)
        {
            if (_tcpServer != null)
            {
                return !_tcpServer.Listening;
            }
            else
            {
                return false;
            }
        }

        private void StopListen(object parameter)
        {
            try
            {
                _tcpServer.StopListening();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private bool CanStopListen(object parameter)
        {
            if(_tcpServer != null)
            {
                return _tcpServer.Listening;
            }
            else
            {
                return false;
            }
        }

        private void StartBind(object parameter)
        {
            try
            {
                if (_tcpClient == null)
                {
                    _tcpClient = new TcpSharpSocketClient();
                }

                if (_tcpClient.Connected)
                {
                    _tcpClient.Disconnect();
                }

                _tcpClient.Host = _mainModel.ClientSettings.IpAddress;
                _tcpClient.Port = _mainModel.ClientSettings.Port;

                _tcpClient.OnConnected += (sender, e) =>
                {
                    UpdateLogText($"Client Connected to {e.ServerHost}:{e.ServerPort}");
                };
                _tcpClient.OnError += (sender, e) =>
                {
                    UpdateLogText($"Client Error: {e.Exception.Message}");
                };
                _tcpClient.OnDisconnected += (sender, e) =>
                {
                    UpdateLogText($"Client Disconnect: {e.Reason}");
                };
                _tcpClient.OnReconnected += (sender, e) =>
                {
                    UpdateLogText($"Client Reconnect to: {e.ServerIPAddress}:{e.ServerPort}");
                };

                _tcpClient.Connect();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private bool CanStartBind(object parameter)
        {
            if (_tcpClient != null)
            {
                if(_tcpClient.Connected)
                {
                    return false;
                }
                else
                { 
                    return true; 
                }
            }
            else
            {
                return false;
            }
        }

        private void StopBind(object parameter)
        {
            try
            {
                _tcpClient.Disconnect();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }

        private bool CanStopBind(object parameter)
        {
            if (_tcpClient != null)
            {
                return _tcpClient.Connected;
            }
            else
            {
                return false;
            }
        }

        private void CleanScreen(object parameter)
        {
            MainModel.OutcomeText.RemoveAt(0);
        }

        private void UpdateButtonStates()
        {
            OnPropertyChanged(nameof(IsListenEnabled));
            OnPropertyChanged(nameof(IsStopListenEnabled));
            OnPropertyChanged(nameof(IsBindEnabled));
            OnPropertyChanged(nameof(IsStopBindEnabled));
        }

        public bool IsListenEnabled => _isListenEnabled;
        public bool IsStopListenEnabled => _isStopListenEnabled;
        public bool IsBindEnabled => _isBindEnabled;
        public bool IsStopBindEnabled => IsStopBindEnabled;
    }
}
