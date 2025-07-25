﻿using System;
using System.Windows.Forms;
using TcpSharp;

namespace MastercardHost
{
    public partial class TestForm : Form
    {
        private TcpSharpSocketServer _tcpServer;
        private TcpSharpSocketClient _tcpClient;
        private string _connectID;
        private int _port;

        public TestForm(MainForm mainForm)
        {
            InitializeComponent();
            //_tcpServer = new TcpSharpSocketServer();
            _tcpClient = new TcpSharpSocketClient("localhost", 6908);
            _tcpClient.OnConnected += (sender, e) =>
            {
                MyLogManager.Log("Client connected to server");
            };
            _tcpClient.OnError += (sender, e) =>
            {
                MyLogManager.Log($"Client error: {e.ToString()}");
            };
            _tcpClient.Connect();
        }

        private void TestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_tcpServer != null && _tcpServer.Listening)
                {
                    _tcpServer.StopListening();
                }
                if (_tcpClient != null && _tcpClient.Connected)
                { 
                    _tcpClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Close TestForm Exception: {ex.Message}"); ;
            }
        }

        private void button_ACT_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"": ""ACT"",
                                    ""signalData"": [
                                        { ""id"": ""9F02"", ""value"": ""000000001500"" },
                                        { ""id"": ""5F57"", ""value"": null },
                                        { ""id"": ""9F03"", ""value"": null },
                                        { ""id"": ""DF8104"", ""value"": null },
                                        { ""id"": ""DF8105"", ""value"": null },
                                        { ""id"": ""9F7C"", ""value"": ""1122334455667788990011223344556677889900"" },
                                        { ""id"": ""9F53"", ""value"": ""46"" },
                                        { ""id"": ""5F2A"", ""value"": ""0978"" },
                                        { ""id"": ""5F36"", ""value"": ""02"" },
                                        { ""id"": ""9A"", ""value"": ""250514"" },
                                        { ""id"": ""9F21"", ""value"": ""195212"" },
                                        { ""id"": ""9C"", ""value"": ""00"" }
                                    ]
                                }";
                //_tcpServer.SendString(_connectID, data);
                _tcpClient.SendString(data);
                MyLogManager.Log($"client send data:{data}");
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_CLEAN_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""CLEAN"", 
                                    ""signalData"":[
                                         {""id"":""9A"", ""value"":""170303""},
                                         {""id"":""9F21"", ""value"":""133333""}
                                        ]
                                }";

                _tcpServer.SendString( _connectID, data);
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_CONFIG_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""CONFIG"", 
                                    ""signalData"":[
                                        {""id"":""CONF_NAME"", ""value"":""PPS_MChip1""}
                                    ]
                                }";

                _tcpServer.SendString(_connectID, data);
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_DET_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""DET"", 
                                    ""signalData"":[
                                        {""id"":""DET"",""value"":""DF81120182""}
                                    ]
                                }";

                _tcpServer.SendString(_connectID, data);
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_RUNTEST_RESULT_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""RUNTEST_ RESULT"",
                                    ""signalData"":[
                                        {
                                            ""id"":""TestResult"", 
                                            ""value"":""PASS""
                                        }
                                    ]
                                }";

                _tcpServer.SendString (_connectID, data);

            }
            catch(Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_STOP_Click(object sender, EventArgs e)
        {

        }

        private void button_TEST_INFO_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{""signalType"":""TEST_INFO"", ""signalData"":[
                                     {""id"":""InterfaceVersion"", ""value"":""20170303""},
                                     {""id"":""SessionId"", ""value"":""Session_20150829_093731""},
                                     {""id"":""TestId"", ""value"":""3GX2-1600-03""},
                                     {""id"":""DekDetId"", ""value"":""3MX2-2500""}
                                    ]}";

                _tcpServer.SendString ( _connectID, data);
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if(_tcpServer.Listening)
                {
                    _tcpServer.StopListening();
                }

                if (int.TryParse(this.textBox_Port.Text, out _port))
                {

                }
                else
                {
                    MyLogManager.Log($"Parse Port to INT error");

                    // Replace the problematic line with the following:
                    MessageBox.Show("Parse Port to INT error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                _tcpServer.Port = _port;
                _tcpServer.OnStarted += (s, ev) =>
                {
                    MyLogManager.Log($"IN TestForm Server Started Listen on {_tcpServer.Port}");
                };
                _tcpServer.OnError += (s, ev) =>
                {
                    MyLogManager.Log($"IN TestForm Server Error: {ev.Exception.Message}");
                };
                _tcpServer.OnStopped += (s, ev) =>
                {
                    MyLogManager.Log($"IN TestForm Server Stop Listen: {ev.IsStopped}");
                };
                _tcpServer.OnConnected += (s, ev) =>
                {
                    _connectID = ev.ConnectionId;
                    MyLogManager.Log($"IN TestForm Server Connect on {ev.IPAddress}:{ev.Port}, Connect ID is: {ev.ConnectionId}");
                };
                _tcpServer.OnDataReceived += (s, ev) =>
                {
                    MyLogManager.Log($"Server Receive Data: {ev.Data.ToString()}");
                };


                _tcpServer.StartListening();
            }
            catch(Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        public int Port 
        { 
            get => _port;
            private set => _port = value;
        }
    }
}
