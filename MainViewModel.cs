using System;
using MvvmHelpers;
using MvvmHelpers.Commands;
using System.Windows;
using System.IO.Ports;
using TcpSharp;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using MastercardHost.MessageProtos;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace MastercardHost
{
    public class MainViewModel : BaseViewModel
    {

        public Command startListenCommand { get; }
        public Command stopListenCommand { get; }
        public Command clearScreenCommand { get; }
        public Command OpenSerialPortCommand { get; }
        public Command CloseSerialPortCommand { get; }

        private TcpSharpSocketServer _tcpServer;

        private bool _isListenEnabled;
        private bool _isStopListenEnabled;
        private bool _isBindEnabled;
        private bool _isStopBindEnabled;

        private int _server_port;
        private string _server_ipAddr;

        private string _respCode;
        private string _iad;
        private string _script;
        private ObservableRangeCollection<string> _outcomeText;

        private int _outcomeLimit;
        private string _connectionID;

        private SerialPort _serialPort;
        //串口属性
        private string _selectedPortName;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;

        private string _currConfig;
        private string _selectConfig;
        private bool _isOpenSerialEnabled;
        private bool _isCloseSerialEnabled;

        private int _capkCounter;

        public MainViewModel()
        {
            _tcpServer = new TcpSharpSocketServer();
            _serialPort = new SerialPort();

            _tcpServer.OnDataReceived += (sender, e) =>
            {
                string message = Encoding.UTF8.GetString(e.Data);
                ProcessFromTestTool(message);
            };
            _tcpServer.OnConnected += (sender, e) => 
            {
                _connectionID = e.ConnectionId;
                UpdateLogText($"Connect on {e.IPAddress}:{e.Port}");
                UpdateLogText($"Connect ID is: {e.ConnectionId}");
                MyLogManager.Log($"Connect on {e.IPAddress}:{e.Port}");
                MyLogManager.Log($"Connect ID is: {e.ConnectionId}");
            };
            _tcpServer.OnDisconnected += (sender, e) =>
            {
                UpdateLogText($"{e.ConnectionId} disconnect");
                UpdateLogText($"Reason: {e.Reason}");
                MyLogManager.Log($"{e.ConnectionId} disconnect");
                MyLogManager.Log($"Reason: {e.Reason}");
            };
            _tcpServer.OnError += (sender, e) =>
            {
                UpdateLogText($"{e.ConnectionId} Error occur");
                UpdateLogText($"Error: {e.Exception.Message}");
                MyLogManager.Log($"{e.ConnectionId} Error occur");
                MyLogManager.Log($"Error: {e.Exception.Message}");
            };
            _tcpServer.OnStarted += (sender, e) =>
            {
                UpdateLogText($"Server Started Listen");
            };
            _tcpServer.OnStopped += (sender, e) =>
            {
                UpdateLogText($"Server Stop Listen");
            };

            startListenCommand = new Command(StartListen);
            stopListenCommand = new Command(StopListen);
            clearScreenCommand = new Command(CleanScreen);
            OpenSerialPortCommand = new Command(OpenSerialPort);
            CloseSerialPortCommand = new Command(CloseSerialPort);

            _isListenEnabled = true;
            _isBindEnabled = true;
            _isStopListenEnabled = false;
            _isStopBindEnabled = false;

            _server_port = 6908;
            _server_ipAddr = "127.0.0.1";

            _respCode = "00";
            _iad = "";
            _script = "";
            _outcomeText = new ObservableRangeCollection<string>();

            _baudRate = 115200;
            _parity = Parity.None;
            _stopBits = StopBits.One;
            _dataBits = 8;

            _isOpenSerialEnabled = true;
            _isCloseSerialEnabled = false;

            _capkCounter = 0;
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

        public string SelectedPortName
        {
            get => _selectedPortName;
            set => SetProperty(ref _selectedPortName, value);
        }

        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        public Parity Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        public int DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        public StopBits StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
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

        public string CurrentConfig
        {
            get => _currConfig;
            set => SetProperty(ref _currConfig, value);
        }

        public bool IsOpenSerialEnabled
        {
            get => _isOpenSerialEnabled;
            set => SetProperty(ref _isOpenSerialEnabled, value);
        }

        public bool IsCloseSerialEnabled
        {
            get => _isCloseSerialEnabled;
            set => SetProperty(ref _isCloseSerialEnabled, value);
        }

        public string SelectConfig
        {
            get => _selectConfig;
            set => SetProperty(ref _selectConfig, value);
        }

        public static ByteString HexStringToByteString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return ByteString.Empty;

            if (hex.Length % 2 != 0)
            {
                MyLogManager.Log($"HexStringToByteString: 无效的十六进制字符串长度：{hex.Length}，输入：'{hex}'");
                throw new ArgumentException("无效的十六进制字符串长度。");
            }

            try
            {
                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    string byteValue = hex.Substring(i * 2, 2);
                    bytes[i] = Convert.ToByte(byteValue, 16);
                }
                return ByteString.CopyFrom(bytes);
            }
            catch(FormatException ex)
            {
                MyLogManager.Log($"HexStringToByteString: 无效的十六进制字符串 '{hex}' - {ex.Message}");
                throw new FormatException($"无效的十六进制字符串: '{hex}'", ex);
            }
        }

        public static bool StringToBool(string val)
        {
            if (string.IsNullOrEmpty(val))
                return false;

            return val == "1" || val == "01";
        }

        public static byte[] HexStringToByteArray(String hexString)
        {
            if (string.IsNullOrEmpty(hexString))
            {
                return null;
            }

            if (hexString.Length % 2 != 0)
            {
                hexString = "0" + hexString;
            }

            hexString = hexString.ToUpper();
            int length = hexString.Length / 2;
            char[] hexChars = hexString.ToCharArray();
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                int pos = i * 2;
                bytes[i] = (byte)((CharToByte(hexChars[pos]) << 4) | CharToByte(hexChars[pos + 1]));
            }
            return bytes;
        }
        
        private static byte CharToByte(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (byte)(c - '0');
            }
            else if (c >= 'A' && c <= 'F')
            {
                return (byte)(c - 'A' + 10);
            }
            else
            {
                throw new ArgumentException("Invalid hex character: " + c);
            }
        }

        public static string ByteArrayToHexString(byte[] byteArray, int offset, int length)
        {
            if (byteArray == null)
                return null;

            if (offset < 0 || length < 0 || offset + length > byteArray.Length)
                throw new ArgumentOutOfRangeException("Invalid offset or length.");

            StringBuilder hexString = new StringBuilder(length * 2);
            for (int i = offset; i < offset + length; i++)
            {
                hexString.Append(byteArray[i].ToString("X2"));
            }
            return hexString.ToString();
        }

        private static ByteString AsciiStringToByteString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return ByteString.Empty;

            return ByteString.CopyFrom(Encoding.ASCII.GetBytes(str));
        }

        public static bool IsValidHexString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return false;

            if (hex.Length % 2 != 0)
                return false;

            for (int i = 0; i < hex.Length; i++)
            {
                char c = hex[i];
                if (!Uri.IsHexDigit(c))
                    return false;
            }

            return true;
        }

        private static string GetSafeString(string input)
        {
            return input ?? string.Empty;
        }



        public void UpdateLogText(string text)
        {
            _outcomeText.Add(text+Environment.NewLine);
            //TODO: 添加通过菜单控制自动清log的行数限制
            if (_outcomeText.Count > 100)
            {
                _outcomeText.Clear();
            }
        }

        private void StartListen(object parameter)
        {
            try
            {
                _tcpServer.Port = _server_port;
                _tcpServer.StartListening();
                IsStopListenEnabled = true;
                IsListenEnabled = false;
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopListen(object parameter)
        {
            try
            {
                _tcpServer.StopListening();
                IsStopListenEnabled= false;
                IsListenEnabled = true;
            }
            catch (Exception ex)
            {
                // 显示弹窗提示用户
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CleanScreen(object parameter)
        {
            _outcomeText.Clear();
        }

        private void OpenSerialPort()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedPortName))
                {
                    MessageBox.Show("请先选择一个串口号！");
                    return;
                }

                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort = new SerialPort(SelectedPortName, BaudRate, Parity, DataBits, StopBits);
                _serialPort.DataReceived += (sender, e) =>
                {
                    int bytesToRead = _serialPort.BytesToRead;
                    byte[] buffer = new byte[bytesToRead];
                    _serialPort.Read(buffer, 0, bytesToRead);
                    ProcessFromPOS(buffer);
                };
                _serialPort.ErrorReceived += (sender, e) =>
                {
                    MessageBox.Show($"串口 {SelectedPortName} 发生错误：{e.EventType}");
                    _serialPort?.Close();
                    IsCloseSerialEnabled = false;
                    IsOpenSerialEnabled = true;
                };

                _serialPort.Open();
                MessageBox.Show($"串口 {SelectedPortName} 已打开！");
                IsOpenSerialEnabled = false;
                IsCloseSerialEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开串口 {SelectedPortName}：{ex.Message}");
            }
        }

        // 关闭串口的方法
        private void CloseSerialPort()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    MessageBox.Show("串口已关闭！");
                    IsOpenSerialEnabled = true;
                    IsCloseSerialEnabled = false;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"无法关闭串口 {SelectedPortName}：{ex.Message}");
            }
        }

        private void TransFormSiganlToPOS(Signal signal)
        {
            try
            {
                SignalProtocol signalProtocol = new SignalProtocol()
                {
                    Type = signal.signalType,
                };

                foreach (var tag in signal.signalData)
                {
                    MyLogManager.Log($"ID:{tag.id}, Value:{tag.value ?? "null"}");
                    SignalDataProtocol dataProtocol = new SignalDataProtocol
                    {
                        Id = tag.id,
                        Value = GetSafeString(tag.value)
                    };
                    signalProtocol.Data.Add(dataProtocol);
                }

                Envelope envelope = new Envelope()
                {
                    Signal = signalProtocol
                };

                byte[] serializedData = envelope.ToByteArray();
                _serialPort.Write(serializedData, 0, serializedData.Length);
            }
            catch (ArgumentNullException ex)
            {
                MyLogManager.Log($"TransFormSiganl ArgumentNullException: {ex.Message}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                MyLogManager.Log($"TransFormSiganl InvalidProtocolBufferException: {ex.Message}");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"TransFormSiganl Exception: {ex.Message}");
            }
        }

        private void DownloadConfig(string jsonFilePath)
        {
            MyLogManager.Log("start DownloadConfig");
            MyLogManager.Log($"input jsonFilePath: {jsonFilePath}");

            string jsonContent = File.ReadAllText(jsonFilePath);
            JObject jsonData = JObject.Parse(jsonContent);

            var aidFieldMapping = new Dictionary<string, Action<AID, string>>()
                                {
                                        { "9F06", (aidMsg, val) => aidMsg.Aid = HexStringToByteString(val) },
                                        { "9C", (aidMsg, val) => aidMsg.TransType = HexStringToByteString(val) },
                                        { "9F09", (aidMsg, val) => aidMsg.AppVer = HexStringToByteString(val) },
                                        { "9F1B", (aidMsg, val) => aidMsg.TermFloorLmt = HexStringToByteString(val) },
                                        { "9F1D", (aidMsg, val) => aidMsg.TermRiskManageData = HexStringToByteString(val) },
                                        { "9F35", (aidMsg, val) => aidMsg.TermType = HexStringToByteString(val) },
                                        { "DF11", (aidMsg, val) => aidMsg.TacDefault = HexStringToByteString(val) },
                                        { "DF12", (aidMsg, val) => aidMsg.TacOnline = HexStringToByteString(val) },
                                        { "DF13", (aidMsg, val) => aidMsg.TacDeny = HexStringToByteString(val) },
                                        { "DF19", (aidMsg, val) => aidMsg.ClFloorLmt = HexStringToByteString(val) },
                                        { "DF20", (aidMsg, val) => aidMsg.ClTransLmt = HexStringToByteString(val) },
                                        { "DF21", (aidMsg, val) => aidMsg.CvmLmt = HexStringToByteString(val) },
                                        { "DF32", (aidMsg, val) => aidMsg.SupStausCheck = StringToBool(val) },
                                        { "DF33", (aidMsg, val) => aidMsg.SupClTransLmtCheck = StringToBool(val) },
                                        { "DF34", (aidMsg, val) => aidMsg.SupClFloorLmtCheck = StringToBool(val) },
                                        { "DF35", (aidMsg, val) => aidMsg.SupTermFloorLmtCheck = StringToBool(val) },
                                        { "DF36", (aidMsg, val) => aidMsg.SupCVMCheck = StringToBool(val) },
                                        { "DF811B", (aidMsg, val) => aidMsg.KernelConf = HexStringToByteString(val) },
                                        { "DF811E", (aidMsg, val) => aidMsg.MsdCVMCapCVMReq = HexStringToByteString(val) },
                                        { "DF8124", (aidMsg, val) => aidMsg.RcTransLmtNoCDCVM = HexStringToByteString(val) },
                                        { "DF8125", (aidMsg, val) => aidMsg.RcTransLmtCDCVM = HexStringToByteString(val) },
                                        { "DF812C", (aidMsg, val) => aidMsg.MsdCVMCapNoCVMReq = HexStringToByteString(val) },
                                        { "9F7E", (aidMsg, val) => aidMsg.MobileSupID = HexStringToByteString(val) },
                                        { "DF811F", (aidMsg, val) => aidMsg.SecueCap = HexStringToByteString(val) },
                                        { "DF8118", (aidMsg, val) => aidMsg.CvmCapCVMReq = HexStringToByteString(val) },
                                        { "DF8119", (aidMsg, val) => aidMsg.CvmCapNoCVMReq = HexStringToByteString(val) },
                                        { "9F40", (aidMsg, val) => aidMsg.AddTermCap = HexStringToByteString(val) },
                                        { "9F33", (aidMsg, val) => aidMsg.TermCap = HexStringToByteString(val) },
                                        { "9F2A", (aidMsg, val) => aidMsg.KernelID = HexStringToByteString(val)},    
                                        // 添加其他映射
                                    };

            var termParamFieldMapping = new Dictionary<string, Action<TermParam, string>>()
                                    {
                                        { "9F01", (termMsg, val) => termMsg.AcquirerID = HexStringToByteString(val) },
                                        { "9F1E", (termMsg, val) => termMsg.IfdSN = AsciiStringToByteString(val) },
                                        { "9F15", (termMsg, val) => termMsg.MerchanCateCode = HexStringToByteString(val) },
                                        { "9F16", (termMsg, val) => termMsg.MerchanID = AsciiStringToByteString(val) },
                                        { "9F4E", (termMsg, val) => termMsg.MerchanName = AsciiStringToByteString(val) },
                                        { "9F1A", (termMsg, val) => termMsg.TermCountryCode = HexStringToByteString(val) },
                                        { "9F1C", (termMsg, val) => termMsg.TermID = AsciiStringToByteString(val) },
                                        { "9F7C", (termMsg, val) => termMsg.MerchanCustData = HexStringToByteString(val) },
                                        { "5F2A", (termMsg, val) => termMsg.TransCurrCode = HexStringToByteString(val) },
                                        { "5F36", (termMsg, val) => termMsg.TransCurrExp = HexStringToByteString(val) },
                                        { "9F66", (termMsg, val) => termMsg.Ttq = HexStringToByteString(val) },
                                        { "9F53", (termMsg, val) => termMsg.TransCateCode = HexStringToByteString(val) },
                                        { "DF811A", (termMsg, val) => termMsg.DefualtUDOL = HexStringToByteString(val) },
                                        { "DF810C", (termMsg, val) => termMsg.KernelID = HexStringToByteString(val) },
                                        { "9F6D", (termMsg, val) => termMsg.MsdAppVer = HexStringToByteString(val) },
                                        { "DF811C", (termMsg, val) => termMsg.MaxLifeTornLog = HexStringToByteString(val) },
                                        { "DF811D", (termMsg, val) => termMsg.MaxNumberTornLog = HexStringToByteString(val) },
                                        { "DF811F", (termMsg, val) => termMsg.SecurCap = HexStringToByteString(val) },
                                        { "8B", (termMsg, val) => termMsg.PoiInfo = HexStringToByteString(val) },
                                        { "9F05", (termMsg, val) => termMsg.ProprietaryTag = HexStringToByteString(val) },
                                        // 添加其他映射
                                    };

            ConfigProtocol configProtocol = new ConfigProtocol();

            JArray aidParamArray = (JArray)jsonData["AIDParam"];

            foreach (JObject aidItem in aidParamArray)
            {
                AID aidMsg = new AID();

                foreach (var property in aidItem.Properties())
                {
                    string key = property.Name;
                    JToken valueToken = property.Value;

                    //MyLogManager.Log($"key = {key}");

                    if (aidFieldMapping.ContainsKey(key))
                    {
                        string value = valueToken?.Value<string>();
                        //MyLogManager.Log($"value = {value}");
                        //if(key.Equals("9F33"))
                        //{
                        //    MyLogManager.Log("///////////////////////////////");
                        //}
                        aidFieldMapping[key](aidMsg, value);
                    }
                    else
                    {
                        //TODO: 处理其他可能的扩展字段
                    }
                }

                configProtocol.Aid.Add(aidMsg);
            }

            // 处理 TermParam 对象
            JObject termParamObj = (JObject)jsonData["TermParam"];
            TermParam termParamMsg = new TermParam();

            foreach (var property in termParamObj.Properties())
            {
                string key = property.Name;
                JToken valueToken = property.Value;

                if (termParamFieldMapping.ContainsKey(key))
                {
                    string value = valueToken?.Value<string>();
                    termParamFieldMapping[key](termParamMsg, value);
                }
                else
                {
                    //TODO: 处理其他可能的扩展字段
                }
            }

            configProtocol.Termpar = termParamMsg;

            Envelope envelope = new Envelope()
            {
                Config = configProtocol
            };

            byte[] serializedData = envelope.ToByteArray();

            MyLogManager.Log($"Download Config data len:  {serializedData.Length}");
            _serialPort.Write(serializedData, 0, serializedData.Length);
        }

        private void DownloadCAPK(string jsonFilePath)
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            JObject jsonData = JObject.Parse(jsonContent);

            var capkFieldMapping = new Dictionary<string, Action<CAPK, string>>()
                                {
                                        { "9F06", (capkMsg, val) => capkMsg.Rid = HexStringToByteString(val) },
                                        { "9F22", (capkMsg, val) => capkMsg.Index = HexStringToByteString(val) },
                                        { "DF04", (capkMsg, val) => capkMsg.Exponent = HexStringToByteString(val) },
                                        { "DF02", (capkMsg, val) => capkMsg.Modul = HexStringToByteString(val) },
                                        { "DF03", (capkMsg, val) => capkMsg.Checksum = HexStringToByteString(val) },
                                        { "DF05", (capkMsg, val) => capkMsg.Expdate = HexStringToByteString(val) },
                                        { "DF06", (capkMsg, val) => capkMsg.Hashind = HexStringToByteString(val) },
                                        { "DF07", (capkMsg, val) => capkMsg.Arithind = HexStringToByteString(val) },
                                    };

            //CAPKList capkList = new CAPKList();
            List<CAPK> capkList = new List<CAPK>();
            bool flag=false;

            JArray capkParamArray = (JArray)jsonData["CAPKParam"];
            foreach (JObject capkItem in capkParamArray)
            {
                CAPK capkMsg = new CAPK();

                foreach (var property in capkItem.Properties())
                {
                    string key = property.Name;
                    MyLogManager.Log($"Processing CAPK field: {key}");

                    JToken valueToken = property.Value;

                    if (capkFieldMapping.ContainsKey(key))
                    {
                        string value = valueToken?.Value<string>();
                        MyLogManager.Log($"Processing CAPK field: {key}, value: {value}");

                        if (string.IsNullOrEmpty(value))
                        {
                            MyLogManager.Log($"CAPK field {key} has null or empty value, setting to default");
                            capkFieldMapping[key](capkMsg, null);
                        }
                        else
                        {
                            //如果数据格式非Hex则跳过不处理
                            if (!IsValidHexString(value))
                            {
                                MyLogManager.Log($"Invalid hex string for field {key}: '{value}'");
                                //continue;
                            }
                            try
                            {
                                capkFieldMapping[key](capkMsg, value);
                            }
                            catch (Exception ex)
                            {
                                MyLogManager.Log($"Error processing CAPK field {key} with value '{value}': {ex.Message}");
                                // 根据需要处理错误
                            }
                        }
                    }
                    else
                    {
                        // 处理未知字段
                        MyLogManager.Log($"Unknown CAPK field: {key}");
                    }
                }

                if (_capkCounter == 0)
                {
                    if (capkItem["9F06"]?.Value<string>() == "A000000004")
                    {
                        capkList.Add(capkMsg);
                    }
                }
                else if(_capkCounter == 1) 
                {
                    if (capkItem["9F06"]?.Value<string>() == "B012345678")
                    {
                        capkList.Add(capkMsg);
                    }
                }
            }

            if (_capkCounter == 0)
            {
                flag = false;
                _capkCounter++;
            }
            else if (_capkCounter == 1)
            {
                flag = true;
                _capkCounter = 0;
            }

            CAPKList capkSendList = new CAPKList()
            { 
                Capk = { capkList },
                IsFinish = flag
            };

            Envelope envelope = new Envelope()
            {
                CapkList = capkSendList,
            };

            byte[] serializedData = envelope.ToByteArray();

            MyLogManager.Log($"Download CAPK:  {envelope.ToString()}");
            _serialPort.Write(serializedData, 0, serializedData.Length);
        }

        private void DownloadRevokey(string jsonFilePath)
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            JObject jsonData = JObject.Parse(jsonContent);

            var revokeyFieldMapping = new Dictionary<string, Action<REVOPK, string>>()
                                {
                                        { "9F06", (revokeyMsg, val) => revokeyMsg.Rid = HexStringToByteString(val) },
                                        { "8F", (revokeyMsg, val) => revokeyMsg.Index = HexStringToByteString(val) },
                                        { "DF8105", (revokeyMsg, val) => revokeyMsg.Csn = HexStringToByteString(val) },
                                    };

            REVOPKList revopkList = new REVOPKList();

            JArray capkParamArray = (JArray)jsonData["RevoPKParam"];
            foreach (JObject capkItem in capkParamArray)
            {
                REVOPK revokeyMsg = new REVOPK();

                foreach (var property in capkItem.Properties())
                {
                    string key = property.Name;
                    JToken valueToken = property.Value;

                    if (revokeyFieldMapping.ContainsKey(key))
                    {
                        string value = valueToken?.Value<string>();
                        revokeyFieldMapping[key](revokeyMsg, value);
                    }
                    else
                    {
                        //TODO: 处理其他可能的扩展字段
                    }
                }

                revopkList.Revopk.Add(revokeyMsg);
            }

            Envelope envelope = new Envelope()
            {
                RevopkList = revopkList,
            };

            byte[] serializedData = envelope.ToByteArray();

            MyLogManager.Log($"Download Revokey:  {serializedData}");
            _serialPort.Write(serializedData, 0, serializedData.Length);
        }

        private void ProcessFromTestTool(string receiveData)
        {
            try
            {
                Signal signal = JsonConvert.DeserializeObject<Signal>(receiveData);

                MyLogManager.Log($"Received {signal.signalType} signal");

                MyLogManager.Log($"Received Data: {Environment.NewLine}{receiveData}");

                switch (signal.signalType)
                {
                    case "ACT":
                        TransFormSiganlToPOS(signal);
                        break;

                    case "CONFIG":
                        var configName = signal.signalData.FirstOrDefault(sd => sd.id == "CONF_NAME");
                        if (configName != null && configName.value != null)
                        {
                            string fileName = configName.value + ".json";
                            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                            string configDir = runDir + "Config\\Config\\";
                            MyLogManager.Log($"Config Dir:{configDir}");

                            if (Directory.Exists(configDir))
                            {
                                MyLogManager.Log($"Target Config:{configName.value}");
                                MyLogManager.Log($"Current Config:{CurrentConfig}");

                                if (!File.Exists(configDir + fileName))
                                {
                                    MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    MyLogManager.Log($"Target Config{configDir + fileName} doesn't exist");
                                }
                                else
                                {
                                    if (CurrentConfig == null || CurrentConfig == "" || CurrentConfig != configName.value)
                                    {
                                        CurrentConfig = configName.value;
                                        DownloadConfig(configDir + fileName);
                                    }
                                    else
                                    {
                                        Signal signal_config_ack = new Signal()
                                        {
                                            signalType = "CONFIG_ACK",
                                        };
                                        SignalData signalData = new SignalData()
                                        {
                                            id = "ResponseCode",
                                            value = "YES",
                                        };
                                        signal_config_ack.signalData.Add(signalData);

                                        string jstr = JsonConvert.SerializeObject(signal_config_ack);
                                        MyLogManager.Log($"Send to TestTool: {jstr}");
                                        _tcpServer.SendString(_connectionID, jstr);
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        break;

                    case "CLEAN":
                        var date = signal.signalData.FirstOrDefault(s => s.id == "9A");
                        if (date != null && date.value != null)
                        {
                            MyLogManager.Log($"9A:  {date.value}");
                        }

                        var time = signal.signalData.FirstOrDefault(s => s.id == "9F21");
                        if (time != null && time.value != null)
                        {
                            MyLogManager.Log($"9F21:   {time.value}");
                        }
                        TransFormSiganlToPOS(signal);
                        break;

                    case "DET":
                        var det = signal.signalData.FirstOrDefault(s => s.id == "DET");
                        if (det != null && det.value != null)
                        {
                            MyLogManager.Log($"DET:  {det.value}");
                        }
                        TransFormSiganlToPOS(signal);
                        break;

                    case "RUNTEST_ RESULT":
                        var testResult = signal.signalData.FirstOrDefault(s => s.id == "TestResult");
                        if (testResult != null && testResult.value != null)
                        {
                            MyLogManager.Log($"TestResult:  {testResult.value}");
                        }
                        TransFormSiganlToPOS(signal);
                        break;

                    case "TEST_INFO":
                        foreach (var id in signal.signalData)
                        {
                            MyLogManager.Log($"{id.id}:  {id.value}");
                        }
                        TransFormSiganlToPOS(signal);
                        break;

                    case "STOP":
                        TransFormSiganlToPOS(signal);
                        break;


                    default:
                        MyLogManager.Log("无法识别的Signal类型");
                        break;
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"ProcessFromTestTool Exception: {ex.Message}");
            }
        }

        private void TransformSignalToTestTool(SignalProtocol signalProtocol)
        {
            JObject root = new JObject();
            root.Add("signalType", signalProtocol.Type);

            JArray signalDataArray = new JArray();
            foreach (var data in signalProtocol.Data)
            {
                JObject dataObj = new JObject();
                dataObj.Add("id", data.Id);

                if (string.IsNullOrEmpty(data.Value))
                {
                    dataObj.Add("value", null);
                }
                else
                {
                    dataObj.Add("value", data.Value);
                }

                signalDataArray.Add(dataObj);
            }

            root.Add("signalData", signalDataArray);

            MyLogManager.Log($"Send to TestTool: {root.ToString()}");
            _tcpServer.SendString(_connectionID, root.ToString());
        }

        private void ShowOutcome(string outcome)
        {
            byte[] bytes = HexStringToByteArray(outcome);

            if(bytes != null)
            {
                UpdateLogText("Outcome:");
                //Display Status
                switch(bytes[0])
                {
                    case 0x10:
                        UpdateLogText("Status:  APPROVED");
                        break;
                    case 0x20:
                        UpdateLogText("Status:  DECLINED");
                        break;
                    case 0x30:
                        UpdateLogText("Status:  ONLINE REQUEST");
                        break;
                    case 0x40:
                        UpdateLogText("Status:  END APPLICATION");
                        break;
                    case 0x50:
                        UpdateLogText("Status:  SELECT NEXT");
                        break;
                    case 0x60:
                        UpdateLogText("Status:  TRY ANOTHER INTERFACE");
                        break;
                    case 0x70:
                        UpdateLogText("Status:  TRY AGAIN");
                        break;
                    case 0xF0:
                        UpdateLogText("Status:  N/A");
                        break;
                    default:
                        UpdateLogText("Status:  RFU");
                        break;
                }
                //Display Start
                switch (bytes[1])
                {
                    case 0x00:
                        UpdateLogText("Start:  A");
                        break;
                    case 0x10:
                        UpdateLogText("Start:  B");
                        break;
                    case 0x20:
                        UpdateLogText("Start:  C");
                        break;
                    case 0x30:
                        UpdateLogText("Start:  D");
                        break;
                    case 0xF0:
                        UpdateLogText("Start:  N/A");
                        break;
                    default:
                        UpdateLogText("Start:  RFU");
                        break;
                }
                //Display Online Response Data
                switch(bytes[2])
                {
                    case 0xF0:
                        UpdateLogText("Online Response Data:  N/A");
                        break;
                    default:
                        UpdateLogText("Online Response Data:  RFU");
                        break;
                }
                //Display CVM
                switch (bytes[3])
                { 
                    case 0x00:
                        UpdateLogText("CVM:  NO CVM");
                        break;
                    case 0x10:
                        UpdateLogText("CVM:  OBTAIN SIGNATURE");
                        break;
                    case 0x20:
                        UpdateLogText("CVM:  ONLINE PIN");
                        break;
                    case 0x30:
                        UpdateLogText("CVM:  CONFIRMATION CODE VERIFIED");
                        break;
                    case 0xF0:
                        UpdateLogText("CVM:  N/A");
                        break;
                    default:
                        UpdateLogText("CVM:  RFU");
                        break;
                }
                //Display Flag
                if((bytes[4] & 0x80) == 0x80)
                {
                    UpdateLogText("UI Request on Outcome Present: yes");
                }
                else
                {
                    UpdateLogText("UI Request on Outcome Present: no");
                }
                if ((bytes[4] & 0x40) == 0x40)
                {
                    UpdateLogText("UI Request on Restart Present: yes");
                }
                else
                {
                    UpdateLogText("UI Request on Restart Present: no");
                }
                if((bytes[4] & 0x20) == 0x20)
                {
                    UpdateLogText("Data Record Present: yes");
                }
                else
                {
                    UpdateLogText("Data Record Present: no");
                }
                if ((bytes[4] & 0x10) == 0x10)
                {
                    UpdateLogText("Discretionary Data Present: yes");
                }
                else
                {
                    UpdateLogText("Discretionary Data Present: no");
                }
                if ((bytes[4] & 0x08) == 0x08)
                {
                    UpdateLogText("Receipt: yes");
                }
                else
                {
                    UpdateLogText("Receipt: no");
                }
                //Display Alternate Interface Preference
                if (bytes[5] == 0xF0)
                {
                    UpdateLogText("Alternate Interface Preference: N/A");
                }
                else
                {
                    UpdateLogText("Alternate Interface Preference: RFU");
                }
                //Display Field Off Request
                if (bytes[6] == 0xFF)
                {
                    UpdateLogText("Field Off Request: N/A");
                }
                else
                {
                    UpdateLogText("Field Off Request: " + bytes[6].ToString("X2"));
                }
                //Display Removal Timeout
                UpdateLogText("Removal Timeout: " + bytes[7].ToString("X2"));
            }
        }

        private void ShowDataRecord(string dataRecord)
        {
            UpdateLogText("Data Record:");

            if (dataRecord != null)
            {
                TLVObject tLVObject = new TLVObject();

                if (tLVObject.Parse(dataRecord))
                {
                    foreach (var item in tLVObject.TlvDic)
                    {
                        UpdateLogText($"{item.Key}: {item.Value}");
                    }
                }
            }
        }

        private void ShowDiscData(string discData)
        {
            UpdateLogText("Discretionary Data:");

            if (discData != null)
            {
                TLVObject tLVObject = new TLVObject();

                if (tLVObject.Parse(discData))
                {
                    foreach (var item in tLVObject.TlvDic)
                    {
                        UpdateLogText($"{item.Key}: {item.Value}");
                    }
                }
            }
        }

        private void ShowUIReq(string uiReq) 
        {
            byte[] bytes = HexStringToByteArray(uiReq);

            if (bytes != null)
            {
                UpdateLogText("User Interface Request Data:");
                //Display Message Identifier
                switch (bytes[0])
                {
                    case 0x17:
                        UpdateLogText("Message Identifier:  CARD READ OK");
                        break;
                    case 0x21:
                        UpdateLogText("Message Identifier:  TRY AGAIN");
                        break;
                    case 0x03:
                        UpdateLogText("Message Identifier:  APPROVED");
                        break;
                    case 0x1A:
                        UpdateLogText("Message Identifier:  APPROVED – SIGN");
                        break;
                    case 0x07:
                        UpdateLogText("Message Identifier:  DECLINED");
                        break;
                    case 0x1C:
                        UpdateLogText("Message Identifier:  ERROR – OTHER CARD");
                        break;
                    case 0x1D:
                        UpdateLogText("Message Identifier:  INSERT CARD");
                        break;
                    case 0x20:
                        UpdateLogText("Message Identifier:  SEE PHONE");
                        break;
                    case 0x1B:
                        UpdateLogText("Message Identifier:  AUTHORISING – PLEASE WAIT");
                        break;
                    case 0x1E:
                        UpdateLogText("Message Identifier:  CLEAR DISPLAY");
                        break;
                    case 0xFF:
                        UpdateLogText("Message Identifier:  N/A");
                        break;
                    default:
                        UpdateLogText("Message Identifier:  RFU");
                        break;
                }
                //Display Status
                switch (bytes[1])
                {
                    case 0x00:
                        UpdateLogText("Status:  NOT READY");
                        break;
                    case 0x01:
                        UpdateLogText("Status:  IDLE");
                        break;
                    case 0x02:
                        UpdateLogText("Status:  READY TO READ");
                        break;
                    case 0x03:
                        UpdateLogText("Status:  PROCESSING");
                        break;
                    case 0x04:
                        UpdateLogText("Status:  CARD READ SUCCESSFULLY");
                        break;
                    case 0x05:
                        UpdateLogText("Status:  PROCESSING ERROR");
                        break;
                    case 0xFF:
                        UpdateLogText("Status:  N/A");
                        break;
                    default:
                        UpdateLogText("Status:  RFU");
                        break;
                }
                //Display Hold Time
                UpdateLogText("Hold Time: " + bytes[4].ToString("X2"));

                //switch(bytes[13])
                //{
                //    case 0x00:
                //        UpdateLogText("Value Qualifier:  NONE");
                //        break;
                //    case 0x01:
                //        UpdateLogText("Value Qualifier:  AMOUNT");
                //        UpdateLogText($"Value: {ByteArrayToHexString(bytes, 14, 6)}");
                //        break;
                //    case 0x02:
                //        UpdateLogText("Value Qualifier:  BALANCE");
                //        UpdateLogText($"Value: {ByteArrayToHexString(bytes, 14, 6)}");
                //        break;
                //    default:
                //        UpdateLogText("Value Qualifier:  RFU");
                //        break;
                //}
            }
        }

        private void ParseOutSignal(IList<SignalDataProtocol> signalData)
        {
            foreach (var data in signalData) 
            {
                switch(data.Id)
                {
                    case "OPS":
                        ShowOutcome(data.Value);
                        break;
                    case "DataRecord":
                        ShowOutcome(data.Value);
                        break;
                    case "DiscData":
                        ShowDiscData(data.Value);
                        break;
                    case "UIRD":
                        ShowUIReq(data.Value);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ProcessFromPOS(byte[] data)
        {
            MyLogManager.Log($"receive data: " + ByteArrayToHexString(data, 0, data.Length));

            Envelope envelope = Envelope.Parser.ParseFrom(data);
            bool transFlag = false;

            MyLogManager.Log($"envelope.PayloadCase: {envelope.PayloadCase}");

            if (envelope.PayloadCase == Envelope.PayloadOneofCase.Signal)
            {
                SignalProtocol signalProtocol = envelope.Signal;
                MyLogManager.Log($"signalProtocol.Type: {signalProtocol.Type}");

                if (signalProtocol.Type == "OUT" || signalProtocol.Type == "MSG")
                {
                    ParseOutSignal(signalProtocol.Data);
                    transFlag = true;
                }
                else if (signalProtocol.Type == "CONFIG")
                {
                    foreach (var item in signalProtocol.Data)
                    {
                        if (item.Id == "CONF_NAME")
                        {
                            if (SelectConfig != null && SelectConfig != "")
                            {
                                string fileName = SelectConfig + ".json";
                                string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                                string configDir = runDir + "Config\\Config\\";
                                if (Directory.Exists(configDir))
                                {
                                    if (!File.Exists(configDir + fileName))
                                    {
                                        MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        MyLogManager.Log($"Target Config{configDir + fileName} doesn't exist");
                                    }
                                    else
                                    {
                                        DownloadConfig(configDir + fileName);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Config Selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                else if (signalProtocol.Type == "CAPK")
                {
                    string fileName = "PAYPASS_CAPK.json";
                    string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                    string configDir = runDir + "Config\\CAPK\\";
                    MyLogManager.Log($"CAPK Dir:{configDir}");

                    if (Directory.Exists(configDir))
                    {
                        MyLogManager.Log($"Target CAPK:{configDir + fileName}");
                        if (!File.Exists(configDir + fileName))
                        {
                            MessageBox.Show("Target CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            DownloadCAPK(configDir + fileName);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (signalProtocol.Type == "REVOCATION_PK")
                {
                    string fileName = "PAYPASS_Revokey.json";
                    string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                    string configDir = runDir + "Config\\Revocation_CAPK\\";
                    MyLogManager.Log($"Revocation_CAPK Dir:{configDir}");

                    if (Directory.Exists(configDir))
                    {
                        MyLogManager.Log($"Target Revocation_CAPK:{configDir + fileName}");
                        if (!File.Exists(configDir + fileName))
                        {
                            MessageBox.Show("Target Revocation_CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            DownloadRevokey(configDir + fileName);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No Revocation_CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if(signalProtocol.Type == "ACT_ACK" || signalProtocol.Type == "CONFIG_ACK" || signalProtocol.Type == "TEST_INFO_ACK")
                {
                    transFlag = true;
                }
                else
                {
                    MyLogManager.Log("Unrecognized Signal Type");
                }

                if(transFlag)
                {
                    TransformSignalToTestTool(signalProtocol);
                }
            }
            else
            {
                MyLogManager.Log("Unrecognized Protocol Type");
            }
        }

    }
}
