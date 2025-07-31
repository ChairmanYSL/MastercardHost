using Newtonsoft.Json.Linq;
using System;
using System.Linq;
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
                                       { ""id"": ""5F57"", ""value"": null },                                       
                                       { ""id"": ""DF8104"", ""value"": null },  
                                       { ""id"": ""DF8105"", ""value"": null },  
                                       { ""id"": ""9F7C"", ""value"": ""1122334455667788990011223344556677889900"" },  
                                       { ""id"": ""9F53"", ""value"": ""46"" },  
                                       { ""id"": ""5F2A"", ""value"": ""0978"" },  
                                       { ""id"": ""5F36"", ""value"": ""02"" },                                     
                                       { ""id"": ""9F21"", ""value"": ""195212"" }  
                                   ]  
                               }";
                var jsonObject = JObject.Parse(data);
                var signalData = (JArray)jsonObject["signalData"]; // Cast to JArray to access Add method  

                // 处理金额  
                string amount = this.textBox_Amount.Text.Trim();
                MyLogManager.Log($"amount:{amount}");

                if (string.IsNullOrEmpty(amount))
                {
                    amount = "000000000001";
                }
                else
                {
                    amount = new string(amount.Where(char.IsDigit).ToArray());

                    // 如果输入超过12位，截取最后12位
                    if (amount.Length > 12)
                    {
                        amount = amount.Substring(amount.Length - 12);
                    }
                    // 填充到12位
                    amount = amount.PadLeft(12, '0');
                }

                // 处理其他金额  
                string amountOther = this.textBox_OthAmt.Text.Trim();
                amountOther = amountOther.PadLeft(12, '0');

                // 交易类型  
                string transType = this.textBox_TranType.Text.Trim();

                // 添加新字段到signalData数组  
                signalData.Add(new JObject(new JProperty("id", "9F02"), new JProperty("value", amount)));
                signalData.Add(new JObject(new JProperty("id", "9C"), new JProperty("value", transType)));
                signalData.Add(new JObject(new JProperty("id", "9F03"), new JProperty("value", string.IsNullOrEmpty(amountOther) ? null : amountOther)));

                // 添加日期  
                DateTime currentTime = DateTime.Now;
                signalData.Add(new JObject(new JProperty("id", "9A"), new JProperty("value", $"{currentTime:yyMMdd}")));

                // 发送数据  
                string finalData = jsonObject.ToString();
                _tcpClient.SendString(finalData);
                MyLogManager.Log($"client send data:{finalData}");
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

                if (int.TryParse(this.textBox_TranType.Text, out _port))
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
