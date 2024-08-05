﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TcpSharp;

namespace MastercardHost
{
    public partial class TestForm : Form
    {
        private readonly TcpSharpSocketServer _tcpServer;
        private string _connectID;

        public TestForm(MainForm mainForm)
        {
            InitializeComponent();
            _tcpServer = new TcpSharpSocketServer(8766);

            _tcpServer.OnStarted += (sender, e) =>
            {
                MyLogManager.Log($"IN TestForm Server Started Listen on {_tcpServer.Port}");
            };
            _tcpServer.OnError += (sender, e) =>
            {
                MyLogManager.Log($"IN TestForm Server Error: {e.Exception.Message}");
            };
            _tcpServer.OnStopped += (sender, e) =>
            {
                MyLogManager.Log($"IN TestForm Server Stop Listen: {e.IsStopped}");
            };
            _tcpServer.OnConnected += (sender, e) =>
            {
                _connectID = e.ConnectionId;
                MyLogManager.Log($"IN TestForm Server Connect on {e.IPAddress}:{e.Port}, Connect ID is: {e.ConnectionId}");
            };
            _tcpServer.OnDataReceived += (sender, e) =>
            {
                MyLogManager.Log($"Server Receive Data: {e.Data.ToString()}");
            };

            _tcpServer.StartListening();
        }

        private void TestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _tcpServer?.StopListening();
        }

        private void button_ACT_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"": ""ACT"",
                                    ""signalData"": [
                                        { ""id"": ""9F02"", ""value"": ""000000000010"" },
                                        { ""id"": ""5F57"", ""value"": null },
                                        { ""id"": ""9F03"", ""value"": null },
                                        { ""id"": ""DF8104"", ""value"": null },
                                        { ""id"": ""DF8105"", ""value"": null },
                                        { ""id"": ""9F7C"", ""value"": ""0102030405060708091011121314151617181920"" },
                                        { ""id"": ""9F53"", ""value"": ""01"" },
                                        { ""id"": ""5F2A"", ""value"": ""0978"" },
                                        { ""id"": ""5F36"", ""value"": ""02"" },
                                        { ""id"": ""9A"", ""value"": ""170303"" },
                                        { ""id"": ""9F21"", ""value"": ""133333"" },
                                        { ""id"": ""9C"", ""value"": ""00"" },
                                        { ""id"": ""DF8112"", ""value"": ""00"" },
                                        { ""id"": ""DF9901"", ""value"": ""010203"" }
                                    ]
                                }";
                _tcpServer.SendString(_connectID, data);
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
    }
}
