using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TcpSharp;

namespace MastercardHost
{
    public class TcpCommunication
    {
        private TcpSharpSocketServer _tcpServer;
        private TcpSharpSocketClient _tcpClient;

        public event EventHandler<OnClientDataReceivedEventArgs> OnClientDataReceived;
        public event EventHandler<OnServerDataReceivedEventArgs> OnServerDataReceived;

        private string _connectionId;
        public string ConnectionID
        {
            get => _connectionId;
            private set => _connectionId = value;
        }

        public void StartServer(int port, Action<string> logAction)
        {
            _tcpServer = new TcpSharpSocketServer();
            _tcpServer.Port = port;

            _tcpServer.OnStarted += (sender, e) =>
            {
                logAction($"Server Started Listen on {port}");
                MyLogManager.Log($"Server Started Listen on {port}");
            };
            _tcpServer.OnError += (sender, e) =>
            {
                logAction($"Server Error: {e.Exception.Message}");
                MyLogManager.Log($"Server Error: {e.Exception.Message}");
            };
            _tcpServer.OnStopped += (sender, e) =>
            {
                logAction("Server Stop Listen");
                MyLogManager.Log($"Server Stop Listen: {e.IsStopped}");
            };
            _tcpServer.OnConnected += (sender, e) =>
            {
                logAction($"Server Connect on {e.IPAddress}:{e.Port}, Connect ID is: {e.ConnectionId}");
                _connectionId = e.ConnectionId;
                MyLogManager.Log($"Server Connect on {e.IPAddress}:{e.Port}, Connect ID is: {e.ConnectionId}");
            };
            _tcpServer.OnDataReceived += (sender, e) =>
            {
                OnServerDataReceived?.Invoke(sender, e);
                MyLogManager.Log($"Server Receive Data: {e.Data.ToString()}");
            };

            _tcpServer.StartListening();
            MyLogManager.Log($"Server Listen Status: {_tcpServer.Listening}");
        }

        public void StopServer()
        {
            _tcpServer?.StopListening();
        }

        public void ConnectClient(string ipAddress, int port, Action<string> logAction)
        {
            MyLogManager.Log($"Input ipAddress: {ipAddress}");
            MyLogManager.Log($"Input port: {port}");

            try
            {
                _tcpClient = new TcpSharpSocketClient
                {
                    Host = ipAddress,
                    Port = port
                };

                _tcpClient.OnConnected += (sender, e) =>
                {
                    logAction($"Client Connected to {e.ServerHost}:{e.ServerPort}");
                    MyLogManager.Log($"Client Connected to {e.ServerHost}:{e.ServerPort}");
                };
                _tcpClient.OnError += (sender, e) =>
                {
                    logAction($"Client Error: {e.Exception.Message}");
                    MyLogManager.Log($"Client Error: {e.Exception.Message}");
                };
                _tcpClient.OnDisconnected += (sender, e) =>
                {
                    logAction($"Client Disconnect: {e.Reason}");
                    MyLogManager.Log($"Client Disconnect: {e.Reason}");
                };
                _tcpClient.OnReconnected += (sender, e) =>
                {
                    logAction($"Client Reconnect to: {e.ServerIPAddress}:{e.ServerPort}");
                    MyLogManager.Log($"Client Reconnect to: {e.ServerIPAddress}:{e.ServerPort}");
                };
                _tcpClient.OnDataReceived += (sender, e) =>
                {
                    OnClientDataReceived?.Invoke(sender, e);
                    MyLogManager.Log($"Client Receive Data: {e.Data.ToString()}");
                };
                
                _tcpClient.Connect();
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Failed to connect: {ex.Message}");
            }
        }

        public void DisconnectClient()
        {
            _tcpClient?.Disconnect();
        }

        public void ClientSendByteArray(byte[] data)
        {
            try
            {
                _tcpClient.SendBytes(data);
                MyLogManager.Log($"ClientSendData: {Encoding.UTF8.GetString(data)}");

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MyLogManager.Log($"ClientSendData Error: {e.Message}");
            }
        }

        public void ClientSendString(string data)
        {
            try
            {
                _tcpClient.SendString(data);
                MyLogManager.Log($"ClientSendData: {data}");
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MyLogManager.Log($"ClientSendData Error: {ex.Message}");
            }
        }

        public void ServerSendByteArray(byte[] data)
        {
            try
            {
                _tcpServer.SendBytes(_connectionId, data);
                MyLogManager.Log($"ServerSendData:{_connectionId} || {Encoding.UTF8.GetString(data)}");
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MyLogManager.Log($"ClientSendData Error: {ex.Message}");
            }
        }

        public void ServerSendString(string data)
        {
            try
            {
                MyLogManager.Log($"ServerSendData:{_connectionId} || {data}");
                _tcpServer.SendString(_connectionId, data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MyLogManager.Log($"ClientSendData Error: {ex.Message}");
            }
        }
    }
}
