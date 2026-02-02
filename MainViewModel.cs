using Google.Protobuf;
using MastercardHost.MessageProtos;
using MvvmHelpers;
using MvvmHelpers.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TcpSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;


namespace MastercardHost
{
    public class ClientInfo
    {
        public string ConnectionId { get; set; }
        public Socket Socket { get; set; }
        public ClientType Type { get; set; } = ClientType.Unknown;
        public DateTime ConnectTime { get; set; } = DateTime.Now;
        // 可以添加更多客户端属性
    }
    public enum ClientType
    {
        Unknown,
        ClientPOS,      // 第一种客户端类型
        ClientTestTool, // 第二种客户端类型
                 // 可以添加更多类型
    }

    public enum CommunicationState
    {
        Idle,
        WaitingForConfigAck,
        WaitingForActAck,
        SerialError,
        Disconnected
    }

    public class SerialOperation
    {
        public string OperationType { get; set; }  // "CONFIG", "ACT", "CAPK", etc.
        public byte[] Data { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime EnqueueTime { get; set; }= DateTime.Now;
        public Action<bool> Callback { get; set; }
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly SynchronizationContext _uiContext;
        public Command startListenCommand { get; }
        public Command stopListenCommand { get; }
        public Command clearScreenCommand { get; }
        public Command OpenSerialPortCommand { get; }
        public Command CloseSerialPortCommand { get; }
        public Command startBindCommand {  get; }
        public Command stopBindCommand { get; }

        private TcpSharpSocketServer _tcpServer;
        private TcpSharpSocketClient _tcpClient;

        private bool _isListenEnabled;
        private bool _isStopListenEnabled;
        private bool _isBindEnabled;
        private bool _isStopBindEnabled;

        private int _server_port;
        private string _server_ipAddr;
        private string _client_ipAddr;

        private string _respCode;
        private string _iad;
        private string _script;
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        public int MaxLogCount { get; set; } = 1000;
        private string _connectionIDTestTool;
        private string _connectionIDPOS;

        private SerialPort _serialPort;
        //private SerialPortWorker _serialWorker;
        //private SimpleSerialQueue _serialWorker;

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
        private bool isTestMode;
        private List<string> _connections;
        private object _lock = new object();
        List<string> connectionsToDisconnect = new List<string>();
        private bool _loopACTFlag = false;
        public event Action<string> OnLoopACTSend;
        private System.Timers.Timer _timer;
        private System.Timers.Timer _configTimer;
        //private Signal _actSignal;
        private int _actResendCounter = 0;
        private int _configResendCounter = 0;
        //private object _downloadLock = new object();
        private bool _isConfigSent = false;
        private bool _isConfigAckReceived = false;
        private bool _isTestInfoReceived = false;
        private ConcurrentQueue<SerialOperation> _queue;
        private Thread _backGround;
        private CancellationTokenSource _cts;  // 用信号量停止线程
        private ManualResetEventSlim _threadStartedEvent = new ManualResetEventSlim(false);
        private List<byte> _serialBuffer = new List<byte>();

        public MainViewModel()
        {
            _uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("必须在UI线程创建ViewModel");
            _tcpServer = new TcpSharpSocketServer();
            _tcpClient = new TcpSharpSocketClient();
            _serialPort = new SerialPort();

            _connections = new List<string>();

            _tcpServer.OnDataReceived += OnDataReceived;
            _tcpServer.OnConnected += (sender, e) =>
            {
                ClearQueue();
                UpdateLogText($"Connect on {e.IPAddress}:{e.Port}");
                UpdateLogText($"Connect ID is: {e.ConnectionId}");
                MyLogManager.Log($"Connect on {e.IPAddress}:{e.Port}");
                MyLogManager.Log($"Connect ID is: {e.ConnectionId}");
                //MyLogManager.Log($"_connections.Count is: {_connections.Count}");

                //测试工具不会主动释放连接，积压太多可能导致无法收到ACT信号，在这里主动断开连接

                _connectionIDTestTool = e.ConnectionId;
                lock(_lock)
                {
                    _connections.Add(e.ConnectionId);
                }

            };
            _tcpServer.OnDisconnected += (sender, e) =>
            {
                UpdateLogText($"{e.ConnectionId} disconnect");
                UpdateLogText($"Reason: {e.Reason}");
                MyLogManager.Log($"{e.ConnectionId} disconnect");
                MyLogManager.Log($"Reason: {e.Reason}");
                lock (_lock)
                {
                    _connections.Remove(e.ConnectionId);
                }
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
                UpdateLogText($"Server Started Listen on {_tcpServer.Port}");
            };
            _tcpServer.OnStopped += (sender, e) =>
            {
                UpdateLogText($"Server Stop Listen");
            };

            _tcpClient.OnDataReceived += (sender, e) =>
            {
                string message = Encoding.UTF8.GetString(e.Data);
                ProcessFromPOS(e.Data);
            };

            startListenCommand = new Command(StartListen);
            stopListenCommand = new Command(StopListen);
            clearScreenCommand = new Command(CleanScreen);
            OpenSerialPortCommand = new Command(OpenSerialPort);
            CloseSerialPortCommand = new Command(CloseSerialPort);
            startBindCommand = new Command(StartBind);
            stopBindCommand = new Command(StopBind);

            _isListenEnabled = true;
            _isBindEnabled = true;
            _isStopListenEnabled = false;
            _isStopBindEnabled = false;

            _server_port = 6908;
            _server_ipAddr = "127.0.0.1";

            _respCode = "00";
            _iad = "";
            _script = "";          

            _baudRate = 115200;
            _parity = Parity.None;
            _stopBits = StopBits.One;
            _dataBits = 8;

            _isOpenSerialEnabled = true;
            _isCloseSerialEnabled = false;

            _capkCounter = 0;
            isTestMode = false;
            _queue = new ConcurrentQueue<SerialOperation>();
            _threadStartedEvent = new ManualResetEventSlim(false);
        }

        private void WorkerLoop(CancellationToken cancellationToken)
        {
            try
            {
                MyLogManager.Log($"串口队列工作线程启动，线程ID: {Thread.CurrentThread.ManagedThreadId}");
                _threadStartedEvent.Set();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 检查取消请求
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // 处理队列逻辑
                        if (!_queue.IsEmpty)
                        {
                            if (_queue.TryPeek(out SerialOperation serialOperation) && serialOperation != null)
                            {
                                // 检查是否已经处理
                                if (serialOperation.RetryCount >= 5)
                                {
                                    _queue.TryDequeue(out _);
                                    continue;
                                }

                                // 检查超时
                                TimeSpan elapsed = DateTime.Now - serialOperation.EnqueueTime;
                                int timeoutSeconds = GetTimeoutByType(serialOperation.OperationType);

                                if (elapsed.TotalSeconds > timeoutSeconds)
                                {
                                    MyLogManager.Log($"{serialOperation.OperationType} 超时 {elapsed.TotalSeconds:F1}秒，重传");

                                    if (_serialPort != null && _serialPort.IsOpen)
                                    {
                                        _serialPort.Write(serialOperation.Data, 0, serialOperation.Data.Length);
                                        serialOperation.RetryCount++;
                                        serialOperation.EnqueueTime = DateTime.Now;
                                    }
                                }
                            }
                        }

                        // 使用CancellationToken的等待，而不是Thread.Sleep
                        // 这样可以在取消时立即响应
                        cancellationToken.WaitHandle.WaitOne(2000);
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消，退出循环
                        break;
                    }
                    catch (ThreadAbortException)
                    {
                        // 线程被强制终止
                        throw;
                    }
                    catch (Exception ex)
                    {
                        MyLogManager.Log($"工作线程处理异常: {ex.Message}");
                        // 短暂休眠避免错误循环
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                MyLogManager.Log("工作线程被强制终止");
                Thread.ResetAbort();  // 防止异常传播
            }
            finally
            {
                MyLogManager.Log($"串口队列工作线程结束，线程ID: {Thread.CurrentThread.ManagedThreadId}");
                _threadStartedEvent.Reset();
            }
        }
        private int GetTimeoutByType(string operationType)
        {
            switch(operationType)
            {
                case "CONFIG":
                    return 6;
                case "TEST_INFO":
                    return 4;
                case "ACT":
                    return 4;
                default:
                    return 3;
            }
        }

        private void _tcpClient_OnDataReceived(object sender, OnClientDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
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

        public string ClientIPAddr
        {
            get => _client_ipAddr;
            set => SetProperty(ref _client_ipAddr, value);
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

        public bool LoopACTFlag
        {
            get => _loopACTFlag;
            set => SetProperty(ref _loopACTFlag, value);
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

            // 转换为小写并去除前后空格  
            string normalizedValue = val.Trim().ToLower();

            // 使用传统的switch语句  
            switch (normalizedValue)
            {
                case "true":
                case "1":
                case "yes":
                case "y":
                case "01":
                    return true;
                case "false":
                case "0":
                case "no":
                case "n":
                case "00":
                    return false;
                default:
                    // 如果以上都不匹配，尝试用bool.Parse（可能会抛出异常）  
                    return bool.Parse(val);
            }
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
            _uiContext.Post(_ =>
            {
                Logs.Add(text);

                // 限制日志条数
                while (Logs.Count > MaxLogCount)
                {
                    Logs.RemoveAt(0);
                }
            }, null);
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
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            Logs.Clear();
        }

        private void OpenSerialPort()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedPortName))
                {
                    System.Windows.MessageBox.Show("请先选择一个串口号！");
                    return;
                }

                if (_serialPort != null && _serialPort.IsOpen)
                {
                    CloseSerialPort();
                }

                _serialPort = new SerialPort(SelectedPortName, BaudRate, Parity, DataBits, StopBits)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    ReadBufferSize = 8192,      // 增大读取缓冲区
                    WriteBufferSize = 8192,
                    ReceivedBytesThreshold = 1, // 收到1字节就触发
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.ErrorReceived += (sender, e) =>
                {
                    System.Windows.MessageBox.Show($"串口 {SelectedPortName} 发生错误：{e.EventType}");
                    _serialPort?.Close();
                    IsCloseSerialEnabled = false;
                    IsOpenSerialEnabled = true;
                };

                _serialPort.DataReceived += (sender, e) =>
                {
                    MyLogManager.Log($"收到串口数据:{_serialPort.BytesToRead}字节");
                    if (_serialPort.BytesToRead > 0 && _serialPort.IsOpen)
                    {
                        byte[] buffer = new byte[_serialPort.BytesToRead];
                        int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
                        MyLogManager.Log($"数据内容：{buffer.ToString()}");
                        MyLogManager.Log($"十六进制数据: {BitConverter.ToString(buffer)}");

                        ProcessFromPOS(buffer);
                    }
                };

                _serialPort.Open();

                ClearQueue();

                StartWorkerThread();

                // 初始化串口工作线程
                System.Windows.MessageBox.Show($"串口 {SelectedPortName} 已打开！");
                IsOpenSerialEnabled = false;
                IsCloseSerialEnabled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开串口失败 {SelectedPortName}：{ex.Message}");
            }
        }

        // 关闭串口的方法
        private void CloseSerialPort()
        {
            try
            {
                MyLogManager.Log("开始关闭串口");

                // 1. 先停止工作线程
                StopWorkerThread();

                // 2. 清空队列
                ClearQueue();

                // 3. 关闭串口
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        MyLogManager.Log("串口已关闭");
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }

                // 4. 更新UI状态
                System.Windows.MessageBox.Show("串口已关闭！");
                IsOpenSerialEnabled = true;
                IsCloseSerialEnabled = false;

                MyLogManager.Log("串口关闭完成");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"关闭串口失败: {ex.Message}");
                System.Windows.MessageBox.Show($"无法关闭串口 {SelectedPortName}：{ex.Message}");
            }
        }
        private void StartBind()
        {
            try
            {
                if (!string.IsNullOrEmpty(_client_ipAddr))
                {
                    _tcpClient.Host = _client_ipAddr;

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"无法绑定到 {SelectedPortName}：{ex.Message}");
            }
        }

        private void StopBind()
        { 
       
        }

        private void OnACKSignalTimeout(bool isTimeout)
        {
            try
            {
                if (_queue.IsEmpty)
                    return;

                // 只处理队首操作
                if (_queue.TryPeek(out SerialOperation serialOperation))
                {
                    if (serialOperation != null && isTimeout)
                    {
                        MyLogManager.Log($"{serialOperation.OperationType} 超时重传，当前重试次数: {serialOperation.RetryCount + 1}");

                        // 重传数据
                        _serialPort.Write(serialOperation.Data, 0, serialOperation.Data.Length);

                        // 更新操作状态
                        serialOperation.RetryCount++;
                        serialOperation.EnqueueTime = DateTime.Now;

                        MyLogManager.Log($"重传完成，更新入队时间: {serialOperation.EnqueueTime:HH:mm:ss}");
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"OnACKSignalTimeout异常: {ex.Message}");
            }
        }
        // 修改发送方法
        private void SendToSerialPort(string type, byte[] data, bool needAck = false)
        {
            MyLogManager.Log($"调用SendToSerialPort: input type{type}\ninput data:{ByteArrayToHexString(data, 0, data.Length)}\n needAck:{needAck}");

            if (_serialPort == null && !_serialPort.IsOpen)
            {
                MyLogManager.Log($"串口未打开，无法发送: {type}");
                return;
            }

            try
            {
                // 决定是否需要ACK
                //bool needsAck = type == "CONFIG" || type == "ACT";
                MyLogManager.Log("needsAck = {need}");

                if (needAck)
                {
                    SerialOperation serialOperation = new SerialOperation();
                    serialOperation.Data = data;
                    serialOperation.OperationType = type;
                    serialOperation.Callback = OnACKSignalTimeout;
                    serialOperation.EnqueueTime = DateTime.Now;
                    // 发送到串口队列
                    _queue.Enqueue(serialOperation);
                    MyLogManager.Log($"已将 {type} 加入串口队列\n当前队列数量:{_queue.Count}");
                    //如果需要ACK的前一个信号还没收到，就先加入队列
                    if(_queue.Count > 0)
                    {

                    }
                    else
                    {
                        _serialPort.Write(data, 0, data.Length);
                    }
                }
                else 
                {
                    MyLogManager.Log($"直接串口发送 {type}");
                    _serialPort.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"发送到串口队列失败: {ex.Message}");
            }
        }

        private void TransFormSiganlToPOS(Signal signal)
        {
            bool needAck = signal.signalType == "CONFIG" || signal.signalType == "ACT" || signal.signalType == "TEST_INFO";
            
            try
            {
                SignalProtocol signalProtocol = new SignalProtocol()
                {
                    Type = signal.signalType,
                };
                MyLogManager.Log($"send signalType: {signal.signalType}");
                //UpdateLogText($"send signalType: {signal.signalType}");

                //signalProtocol.Type += "_HOST";

                foreach (var tag in signal.signalData)
                {
                    MyLogManager.Log($"ID:{tag.id}, Value:{tag.value ?? "null"}");
                    SignalDataProtocol dataProtocol = new SignalDataProtocol
                    {
                        Id = tag.id,
                        Value = GetSafeString(tag.value)
                    };
                    if (tag.id.Equals("ManualTestFlag"))
                    {
                        if (tag.value.Equals("true"))
                        {
                            needAck = false;
                        }
                        else if (tag.value.Equals("loop"))
                        {
                            needAck = true;
                            LoopACTFlag = true;
                        }
                    }
                    signalProtocol.Data.Add(dataProtocol);
                }

                Envelope envelope = new Envelope()
                {
                    Signal = signalProtocol
                };

                //byte[] serializedData = SerializeForNanopbDelimited(envelope);
                byte[] serializedData = envelope.ToByteArray();

                //isTestMode = System.Windows.Forms.Application.OpenForms.OfType<TestForm>().Any();
                //MyLogManager.Log($"isTestMode: {isTestMode}");
                MyLogManager.Log($"send to POS content: {JsonFormatter.Default.Format(envelope)}");

                //if (isTestMode)
                //{
                //    _tcpServer.SendBytes(_connectionIDPOS, serializedData);
                //}
                //else
                //{

                MyLogManager.Log($"调用SendToSerialPort之前: needAck = {needAck}");
                SendToSerialPort(signal.signalType, serializedData, needAck);
                //_serialPort.Write(serializedData, 0, serializedData.Length);
                //int pending = _serialPort.BytesToWrite;
                //MyLogManager.Log($"Bytes pending in buffer: {pending}");
                //}
            }
            catch (ArgumentNullException ex)
            {
                MyLogManager.Log($"TransFormSiganlToPOS ArgumentNullException: {ex.Message}");
            }
            catch (InvalidProtocolBufferException ex)
            {
                MyLogManager.Log($"TransFormSiganlToPOS InvalidProtocolBufferException: {ex.Message}");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"TransFormSiganlToPOS Exception: {ex.Message}");
            }
        }

        private void DiagnoseProtobufTypes()
        {
            try
            {
                MyLogManager.Log("=== 开始诊断 Protobuf 类型 ===");

                // 1. 先检查是否能在 MainViewModel 中创建这些类型
                MyLogManager.Log("1. 测试在主线程创建 Protobuf 实例...");

                try
                {
                    var envelope = new Envelope();
                    MyLogManager.Log("✓ Envelope 创建成功");
                }
                catch (Exception ex)
                {
                    MyLogManager.Log($"✗ Envelope 创建失败: {ex.Message}");
                }

                try
                {
                    var config = new ConfigProtocol();
                    MyLogManager.Log("✓ ConfigProtocol 创建成功");

                    // 检查内部字段
                    if (config.Aid == null)
                        MyLogManager.Log("✗ ConfigProtocol.Aid 为 null");
                    else
                        MyLogManager.Log($"✓ ConfigProtocol.Aid 初始化正常，Count: {config.Aid.Count}");
                }
                catch (Exception ex)
                {
                    MyLogManager.Log($"✗ ConfigProtocol 创建失败: {ex.Message}");
                    MyLogManager.Log($"完整异常: {ex}");
                }

                try
                {
                    var aid = new AID();
                    MyLogManager.Log("✓ AID 创建成功");
                }
                catch (Exception ex)
                {
                    MyLogManager.Log($"✗ AID 创建失败: {ex.Message}");
                }

                try
                {
                    var termParam = new TermParam();
                    MyLogManager.Log("✓ TermParam 创建成功");
                }
                catch (Exception ex)
                {
                    MyLogManager.Log($"✗ TermParam 创建失败: {ex.Message}");
                }

                MyLogManager.Log("=== 诊断完成 ===");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"诊断过程异常: {ex.Message}");
            }
        }

        private byte[] SerializeForNanopbDelimited(Envelope envelope)
        {
            using (var ms = new MemoryStream())
            {
                // 使用 WriteDelimitedTo 对应 nanopb 的 pb_encode_delimited
                envelope.WriteDelimitedTo(ms);
                return ms.ToArray();
            }
        }

        private void DownloadConfig(string jsonFilePath, bool manualDownld = true)
        {
            try
            {
                MyLogManager.Log("start DownloadConfig");
                MyLogManager.Log($"input jsonFilePath: {jsonFilePath}");

                string jsonContent = File.ReadAllText(jsonFilePath);
                JObject jsonData = JObject.Parse(jsonContent);

                var aidFieldMapping = new Dictionary<string, Action<AID, JToken>>()
                                {
                                        { "9F06", (aidMsg, val) => aidMsg.Aid = val.Type != JTokenType.Null ? HexStringToByteString(val.Value<string>()):ByteString.Empty },
                                        { "9C", (aidMsg, val) => aidMsg.TransType = val.Type != JTokenType.Null ? HexStringToByteString(val.Value<string>()):ByteString.Empty },
                                        { "9F09", (aidMsg, val) => aidMsg.AppVer = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F1B", (aidMsg, val) => aidMsg.TermFloorLmt = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F1D", (aidMsg, val) => aidMsg.TermRiskManageData = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F35", (aidMsg, val) => aidMsg.TermType = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF11", (aidMsg, val) => aidMsg.TacDefault = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF12", (aidMsg, val) => aidMsg.TacOnline = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF13", (aidMsg, val) => aidMsg.TacDeny = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF19", (aidMsg, val) => aidMsg.ClFloorLmt = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF20", (aidMsg, val) => aidMsg.ClTransLmt = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF21", (aidMsg, val) => aidMsg.CvmLmt = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF32", (aidMsg, val) => aidMsg.SupStausCheck = val.Type == JTokenType.Null ? false :StringToBool(val.Value<string>()) },
                                        { "DF33", (aidMsg, val) => aidMsg.SupClTransLmtCheck = val.Type == JTokenType.Null ? false :StringToBool(val.Value<string>()) },
                                        { "DF34", (aidMsg, val) => aidMsg.SupClFloorLmtCheck = val.Type == JTokenType.Null ? false : StringToBool(val.Value < string >()) },
                                        { "DF35", (aidMsg, val) => aidMsg.SupTermFloorLmtCheck = val.Type == JTokenType.Null ? false : StringToBool(val.Value < string >()) },
                                        { "DF36", (aidMsg, val) => aidMsg.SupCVMCheck = val.Type == JTokenType.Null ? false : StringToBool(val.Value < string >()) },
                                        { "DF811B", (aidMsg, val) => aidMsg.KernelConf = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF811E", (aidMsg, val) => aidMsg.MsdCVMCapCVMReq = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF8124", (aidMsg, val) => aidMsg.RcTransLmtNoCDCVM = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF8125", (aidMsg, val) => aidMsg.RcTransLmtCDCVM = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF812C", (aidMsg, val) => aidMsg.MsdCVMCapNoCVMReq = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F7E", (aidMsg, val) => aidMsg.MobileSupID = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF811F", (aidMsg, val) => aidMsg.SecueCap = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF8118", (aidMsg, val) => aidMsg.CvmCapCVMReq = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF8119", (aidMsg, val) => aidMsg.CvmCapNoCVMReq = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F40", (aidMsg, val) => aidMsg.AddTermCap = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F33", (aidMsg, val) => aidMsg.TermCap = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F2A", (aidMsg, val) => aidMsg.KernelID = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},    
                                        // 添加其他映射
                                    };

                var termParamFieldMapping = new Dictionary<string, Action<TermParam, JToken>>()
                                    {
                                        { "9F01", (termMsg, val) => termMsg.AcquirerID = val.Type != JTokenType.Null ? HexStringToByteString(val.Value<string>()):ByteString.Empty },
                                        { "9F1E", (termMsg, val) => termMsg.IfdSN = val.Type != JTokenType.Null ? AsciiStringToByteString(val.Value<string>()): ByteString.Empty},
                                        { "9F15", (termMsg, val) => termMsg.MerchanCateCode = val.Type != JTokenType.Null ? HexStringToByteString(val.Value<string>()):ByteString.Empty },
                                        { "9F16", (termMsg, val) => termMsg.MerchanID = val.Type != JTokenType.Null ? AsciiStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F4E", (termMsg, val) => termMsg.MerchanName = val.Type != JTokenType.Null ? AsciiStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F1A", (termMsg, val) => termMsg.TermCountryCode = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F1C", (termMsg, val) => termMsg.TermID = val.Type != JTokenType.Null ? AsciiStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F7C", (termMsg, val) => termMsg.MerchanCustData = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "5F2A", (termMsg, val) => termMsg.TransCurrCode = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "5F36", (termMsg, val) => termMsg.TransCurrExp = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F66", (termMsg, val) => termMsg.Ttq = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F53", (termMsg, val) => termMsg.TransCateCode = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF811A", (termMsg, val) => termMsg.DefualtUDOL = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF810C", (termMsg, val) => termMsg.KernelID = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "9F6D", (termMsg, val) => termMsg.MsdAppVer = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF811C", (termMsg, val) => termMsg.MaxLifeTornLog = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF811D", (termMsg, val) => termMsg.MaxNumberTornLog = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF811F", (termMsg, val) => termMsg.SecurCap = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "8B", (termMsg, val) => termMsg.PoiInfo = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF830A", (termMsg, val) => termMsg.ProprietaryTag = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF8308", (termMsg, val) => termMsg.EmptyTagList = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF8309", (termMsg, val) => termMsg.NotPresentTagList = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty },
                                        { "DF8117", (termMsg, val) => termMsg.CardDataInputCap = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF8132", (termMsg, val) => termMsg.RrpMinGrace = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF8133", (termMsg, val) => termMsg.RrpMaxGrace = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF8134", (termMsg, val) => termMsg.RrpExceptCAPDU = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF8135", (termMsg, val) => termMsg.RrpExceptRAPDU = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF8136", (termMsg, val) => termMsg.RrpAccuracyThreshold = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF8112", (termMsg, val) => termMsg.TagsToRead = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF8110", (termMsg, val) => termMsg.Proceed2FirFlg = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "DF810D", (termMsg, val) => termMsg.DsvnTerm = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F5C", (termMsg, val) => termMsg.DsReqOperaID = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F70", (termMsg, val) => termMsg.Envelop1 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F71", (termMsg, val) => termMsg.Envelop2 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F72", (termMsg, val) => termMsg.Envelop3 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F73", (termMsg, val) => termMsg.Envelop4 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F74", (termMsg, val) => termMsg.Envelop5 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F75", (termMsg, val) => termMsg.UnProtectEnvelope1 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F76", (termMsg, val) => termMsg.UnProtectEnvelope2 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F77", (termMsg, val) => termMsg.UnProtectEnvelope3 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F78", (termMsg, val) => termMsg.UnProtectEnvelope4 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "9F79", (termMsg, val) => termMsg.UnProtectEnvelope5 = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "FF8102", (termMsg, val) => termMsg.TagsToWriteBeforeGAC = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},
                                        { "FF8103", (termMsg, val) => termMsg.TagsToWriteAfterGAC = val.Type != JTokenType.Null ? HexStringToByteString(val.Value < string >()) : ByteString.Empty},                                                                  
                                        // 添加其他映射
                                    };

                ConfigProtocol configProtocol = new ConfigProtocol();


                if (jsonData.TryGetValue("AIDParam", out var aidParamToken) && aidParamToken is JArray aidParamArray)
                {
                    foreach (JObject aidItem in aidParamArray)
                    {
                        AID aidMsg = new AID();

                        foreach (var mapping in aidFieldMapping)
                        {
                            if (aidItem.TryGetValue(mapping.Key, out var valueToken))
                            {
                                // 直接传递JToken给映射函数
                                mapping.Value(aidMsg, valueToken);
                            }
                        }

                        configProtocol.Aid.Add(aidMsg);
                    }
                }

                // 处理 TermParam 对象
                if (jsonData.TryGetValue("TermParam", out var termParamToken) && termParamToken is JObject termParamObj)
                {
                    TermParam termParamMsg = new TermParam();

                    foreach (var mapping in termParamFieldMapping)
                    {
                        if (termParamObj.TryGetValue(mapping.Key, out var valueToken))
                        {
                            mapping.Value(termParamMsg, valueToken);
                        }
                    }

                    configProtocol.Termpar = termParamMsg;
                }

                Envelope envelope = new Envelope()
                {
                    Config = configProtocol
                };

                byte[] serializedData = envelope.ToByteArray();

                MyLogManager.Log($"Download Config data:\n");
                LogFormattedProtobuf(envelope);
                //isTestMode = System.Windows.Forms.Application.OpenForms.OfType<TestForm>().Any();
                //if (isTestMode)
                //{
                //    _tcpServer.SendBytes(_connectionIDPOS, serializedData);
                //}
                //else
                //{
                SendToSerialPort("CONFIG", serializedData, !manualDownld);
                //_serialPort.Write(serializedData, 0, serializedData.Length);
                //}
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"DownloadConfig Exception: {ex.Message}");
                System.Windows.MessageBox.Show($"下载配置时发生错误: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    //MyLogManager.Log($"Processing CAPK field: {key}");

                    JToken valueToken = property.Value;

                    if (capkFieldMapping.ContainsKey(key))
                    {
                        string value = valueToken?.Value<string>();
                        //MyLogManager.Log($"Processing CAPK field: {key}, value: {value}");

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

            MyLogManager.Log($"Download CAPK data:\n");
            LogFormattedProtobuf(envelope);


            //isTestMode = System.Windows.Forms.Application.OpenForms.OfType<TestForm>().Any();
            //if(isTestMode)
            //{
            //    _tcpServer.SendBytes(_connectionIDPOS, serializedData);
            //}
            //else
            //{
            //_serialPort.Write(serializedData, 0, serializedData.Length);
            SendToSerialPort("CAPK", serializedData, false);
            //}
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

            MyLogManager.Log($"Download Revokey data:\n");
            LogFormattedProtobuf(envelope);

            //isTestMode = System.Windows.Forms.Application.OpenForms.OfType<TestForm>().Any();
            //if(isTestMode )
            //{
            //    _tcpServer.SendBytes(_connectionIDPOS, serializedData);
            //}
            //else
            //{
            //_serialPort.Write(serializedData, 0, serializedData.Length);
            //_serialManager.EnqueueAndWaitOperation("REVOPK", serializedData, TimeSpan.FromSeconds(5));
            SendToSerialPort("REVOPK", serializedData, false);
            //}
        }

        private void SendConfigACKSignal()
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

            _tcpServer.SendString(_connectionIDTestTool, jstr);
        }

        private void SendACTACKSignal()
        {
            Signal signal_config_ack = new Signal()
            {
                signalType = "ACT_ACK",
            };
            SignalData signalData = new SignalData()
            {
                id = "ResponseCode",
                value = "YES",
            };
            signal_config_ack.signalData.Add(signalData);

            string jstr = JsonConvert.SerializeObject(signal_config_ack);
            MyLogManager.Log($"Send to TestTool: {jstr}");

            _tcpServer.SendString(_connectionIDTestTool, jstr);

        }

        private void SendTestInfoACKSignal()
        {
            Signal signal_config_ack = new Signal()
            {
                signalType = "TEST_INFO_ACK",
            };
            SignalData signalData = new SignalData()
            {
                id = "ResponseCode",
                value = "YES",
            };
            signal_config_ack.signalData.Add(signalData);

            string jstr = JsonConvert.SerializeObject(signal_config_ack);
            MyLogManager.Log($"Send to TestTool: {jstr}");

            _tcpServer.SendString(_connectionIDTestTool, jstr);
        }

        //private void StartRoundRobinResendACT()
        //{
        //    MyLogManager.Log("Start StartRoundRobinResendACT timer");
        //    _timer = new System.Timers.Timer(5000);
        //    _timer.Elapsed += (s, e) =>
        //    {
        //        _actResendCounter++;
        //        TransFormSiganlToPOS(_actSignal);
        //        MyLogManager.Log($"_actResendCounter = {_actResendCounter}");
        //        if (_actResendCounter > 3)
        //        {
        //            _actResendCounter = 0;
        //            MyLogManager.Log("resend act overlimit,stop timer");

        //            _timer.Stop();
        //            _timer.Dispose();
        //        }
        //    };
        //    _timer.AutoReset = true;
        //    _timer.Start();
        //}

        private void OnDataReceived(object sender, OnServerDataReceivedEventArgs e)
        {
            string receiveData = Encoding.UTF8.GetString(e.Data);
            MyLogManager.Log($"ConnectionId: {e.ConnectionId}");
            try
            {
                if (e.ConnectionId.Equals(_connectionIDTestTool))
                {
                    Signal signal = JsonConvert.DeserializeObject<Signal>(receiveData);
                    if (signal == null)
                    {
                        UpdateLogText("_connectionIDTestTool json data invalid");
                        MyLogManager.Log("_connectionIDTestTool json data invalid");

                        return;
                    }

                    UpdateLogText($"_connectionIDTestTool Received {signal.signalType} signal");
                    MyLogManager.Log($"_connectionIDTestTool Received {signal.signalType} signal");
                    MyLogManager.Log($"Received Data: {Environment.NewLine}{receiveData}");

                    //DiagnoseProtobufTypes();

                    switch (signal.signalType)
                    {
                        case "ACT":
                            SendACTACKSignal();
                            //_actSignal = signal;
                            TransFormSiganlToPOS(signal);

                            //StartRoundRobinResendACT();
                            //HandleActSignal(signal, e.ConnectionId);
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
                                    UpdateLogText($"Target Config:{configName.value}");
                                    UpdateLogText($"Current Config:{CurrentConfig}");

                                    if (!File.Exists(configDir + fileName))
                                    {
                                        System.Windows.MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        MyLogManager.Log($"Target Config{configDir + fileName} doesn't exist");
                                    }
                                    else
                                    {
                                        SendConfigACKSignal();
                                        if (CurrentConfig == null || CurrentConfig == "" || CurrentConfig != configName.value)
                                        {
                                            CurrentConfig = configName.value;
                                            DownloadConfig(configDir + fileName, false);
                                            //StartRoundRobinResendCONFIG();
                                        }
                                    }
                                }
                                else
                                {
                                    System.Windows.MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }

                            break;

                        case "TEST_INFO":
                            foreach (var id in signal.signalData)
                            {
                                MyLogManager.Log($"{id.id}:  {id.value}");
                            }
                            SendTestInfoACKSignal();
                            TransFormSiganlToPOS(signal);
                            //HandleTestInfoSignal(signal, e.ConnectionId);
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

                        case "RUNTEST_RESULT":
                            var testResult = signal.signalData.FirstOrDefault(s => s.id == "TestResult");
                            if (testResult != null && testResult.value != null)
                            {
                                MyLogManager.Log($"TestResult:  {testResult.value}");
                            }
                            TransFormSiganlToPOS(signal);
                            break;

                        case "STOP":
                            TransFormSiganlToPOS(signal);
                            break;

                        case "APDU_ACTIVATE":
                            var apdu = signal.signalData.FirstOrDefault(s => s.id == "ACTIVATE");
                            if (apdu != null && apdu.value != null)
                            {
                                MyLogManager.Log($"APDU:  {apdu.value}");
                            }
                            TransFormSiganlToPOS(signal);
                            break;

                        default:
                            MyLogManager.Log("无法识别的Signal类型");
                            break;
                    }
      
                }
                else if (e.ConnectionId.Equals(_connectionIDPOS))
                { 
                    MyLogManager.Log($"_connectionIDPOS receive data: " + ByteArrayToHexString(e.Data, 0, e.Data.Length));

                    Envelope envelope = Envelope.Parser.ParseFrom(e.Data);
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
                                                System.Windows.MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                                MyLogManager.Log($"Target Config{configDir + fileName} doesn't exist");
                                            }
                                            else
                                            {
                                                DownloadConfig(configDir + fileName, false);
                                            }
                                        }
                                        else
                                        {
                                            System.Windows.MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        }
                                    }
                                    else
                                    {
                                        System.Windows.MessageBox.Show("No Config Selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                    System.Windows.MessageBox.Show("Target CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    DownloadCAPK(configDir + fileName);
                                }
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("No CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                    System.Windows.MessageBox.Show("Target Revocation_CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    DownloadRevokey(configDir + fileName);
                                }
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("No Revocation_CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else if (signalProtocol.Type == "ACT_ACK" || signalProtocol.Type == "CONFIG_ACK" || signalProtocol.Type == "TEST_INFO_ACK")
                        {
                            transFlag = true;
                        }
                        else
                        {
                            MyLogManager.Log("Unrecognized Signal Type");
                        }

                        if (transFlag)
                        {
                            TransformSignalToTestTool(signalProtocol, true);
                        }
                    }
                    else
                    {
                        MyLogManager.Log("Unrecognized Protocol Type");
                    }   
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"OnDataReceived Exception: {ex.Message}");
            }
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
                                    System.Windows.MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                        
                                        _tcpServer.SendString(_connectionIDTestTool, jstr);
                                    }
                                }
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    case "APDU_ACTIVATE":
                        var apdu = signal.signalData.FirstOrDefault(s => s.id == "ACTIVATE");
                        if (apdu != null && apdu.value != null)
                        {
                            MyLogManager.Log($"APDU:  {apdu.value}");
                        }
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

        private void TransformSignalToTestTool(SignalProtocol signalProtocol, bool disconnectFlag)
        {
            try
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

                _tcpServer.SendString(_connectionIDTestTool, root.ToString());

                if (disconnectFlag)
                {                 
                    lock (_lock)
                    {
                        if (_connections.Count > 10)
                        {
                            // 只保留最新的10个连接
                            int disconnectCount = _connections.Count - 10;

                            // 获取需要断开的最早的连接（前disconnectCount个）
                            for (int i = 0; i < disconnectCount; i++)
                            {
                                connectionsToDisconnect.Add(_connections[i]);
                            }

                            // 从列表中移除这些连接
                            _connections.RemoveRange(0, disconnectCount);
                        }
                    }
                }
                // 在锁外执行断开操作，避免长时间持有锁
                foreach (var connectionId in connectionsToDisconnect)
                {
                    try
                    {
                        var client = _tcpServer.GetClient(connectionId);
                        if (client != null)
                        {
                            _tcpServer.Disconnect(connectionId);
                            MyLogManager.Log($"Disconnected old connection: {connectionId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录异常但继续处理其他连接
                        MyLogManager.Log($"Error disconnecting {connectionId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"TransformSignalToTestTool Exception: {ex.Message}");
            }
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
                UpdateLogText("Online Response Data:  N/A");
                //switch (bytes[2])
                //{
                //    case 0xF0:
                //        UpdateLogText("Online Response Data:  N/A");
                //        break;
                //    default:
                //        UpdateLogText("Online Response Data:  RFU");
                //        break;
                //}
                //Display CVM
                MyLogManager.Log($"Outcome CVM Byte: {bytes[3].ToString("X2")}");
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
                    UpdateLogText("Receipt: YES");
                }
                else
                {
                    UpdateLogText("Receipt: N/A");
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
                UpdateLogText("_____________________________________");
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
                        if (item.Key.Equals("56"))
                        {
                            UpdateLogText($"{item.Key}(Track 1 Data): {item.Value}");
                            // 解析十六进制字符串
                            string hexString = item.Value;
                            string asciiString = HexToAscii(hexString);

                            UpdateLogText($"  ASCII: {asciiString}");

                            // 解析 Track 1 数据格式
                            ParseTrack1Data(asciiString);
                        }
                        else 
                        {
                            UpdateLogText($"{item.Key}: {item.Value}");
                        }
                    }
                }

                UpdateLogText("_____________________________________");
            }
        }

        private string HexToAscii(string hexString)
        {
            try
            {
                if (string.IsNullOrEmpty(hexString) || hexString.Length % 2 != 0)
                    return "Invalid hex string";

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    string hexChar = hexString.Substring(i, 2);
                    int charValue = Convert.ToInt32(hexChar, 16);

                    // 只显示可打印字符，不可打印字符用'.'代替
                    if (charValue >= 32 && charValue <= 126)
                        sb.Append((char)charValue);
                    else
                        sb.Append('.');
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error converting hex: {ex.Message}";
            }
        }

        // 解析 Track 1 Data
        private void ParseTrack1Data(string track1Data)
        {
            try
            {
                if (string.IsNullOrEmpty(track1Data))
                {
                    UpdateLogText("  Track 1 Data is empty.");
                    return;
                }

                // 查找格式码（第一个字符应该是格式码）
                if (track1Data.Length > 0)
                {
                    char formatCode = track1Data[0];
                    UpdateLogText($"  Format Code: '{formatCode}' ({(int)formatCode:X2})");

                    if (formatCode != 'B')
                    {
                        UpdateLogText($"  Warning: Expected format code 'B' but found '{formatCode}'");
                    }
                }

                // 按照 Track 1 格式解析
                // 格式: Format Code + PAN + '^' + Name + '^' + ExpiryDate + ServiceCode + DiscretionaryData

                // 查找第一个分隔符 '^'
                int firstSeparator = track1Data.IndexOf('^');
                if (firstSeparator < 0)
                {
                    UpdateLogText("  Error: No separator '^' found");
                    return;
                }

                // PAN (主账号) - 从格式码后到第一个'^'之间
                string pan = track1Data.Substring(1, firstSeparator - 1);
                UpdateLogText($"  Primary Account Number (PAN): {pan}");
                UpdateLogText($"  PAN Length: {pan.Length} digits");

                // 查找第二个分隔符 '^'
                int secondSeparator = track1Data.IndexOf('^', firstSeparator + 1);
                if (secondSeparator < 0)
                {
                    UpdateLogText("  Error: No second separator '^' found");
                    return;
                }

                // 姓名 (Name)
                string name = track1Data.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1);
                UpdateLogText($"  Name: {name.Trim()}");

                // 剩余部分: Expiry Date (4) + Service Code (3) + Discretionary Data
                string remainingData = track1Data.Substring(secondSeparator + 1);

                if (remainingData.Length >= 4)
                {
                    // 有效期 (YYMM)
                    string expiryDate = remainingData.Substring(0, 4);
                    UpdateLogText($"  Expiry Date (YYMM): {expiryDate}");

                    // 解析年份和月份
                    if (int.TryParse(expiryDate.Substring(0, 2), out int year) &&
                        int.TryParse(expiryDate.Substring(2, 2), out int month))
                    {
                        // 20XX 年（标准信用卡有效期通常为2000-2099）
                        int fullYear = 2000 + year;
                        UpdateLogText($"  Expiry Date (解析): {fullYear:D4}-{month:D2}");
                    }

                    if (remainingData.Length >= 7)
                    {
                        // 服务代码 (3 digits)
                        string serviceCode = remainingData.Substring(4, 3);
                        UpdateLogText($"  Service Code: {serviceCode}");

                        // 自由数据 (剩余部分)
                        if (remainingData.Length > 7)
                        {
                            string discretionaryData = remainingData.Substring(7);
                            UpdateLogText($"  Discretionary Data: {discretionaryData}");
                            UpdateLogText($"  Discretionary Data Length: {discretionaryData.Length} chars");
                        }
                        else
                        {
                            UpdateLogText($"  Discretionary Data: (none)");
                        }
                    }
                    else
                    {
                        UpdateLogText($"  Warning: Insufficient data for service code");
                    }
                }
                else
                {
                    UpdateLogText($"  Warning: Insufficient data for expiry date");
                }
            }
            catch (Exception ex)
            {
                UpdateLogText($"  Error parsing Track 1 Data: {ex.Message}");
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
                        if (item.Key.Equals("DF8115"))
                        {
                            UpdateLogText("Error Indication:" + item.Value);
                            byte[] bytes = HexStringToByteArray(item.Value);
                            switch (bytes[0])
                            {
                                case 0x00:
                                    UpdateLogText(" L1: OK");
                                    break;
                                case 0x01:
                                    UpdateLogText(" L1: TIME OUT ERROR");
                                    break;
                                case 0x02:
                                    UpdateLogText(" L1: TRANSMISSION ERROR");
                                    break;
                                case 0x03:
                                    UpdateLogText(" L1: PROTOCOL ERROR");
                                    break;
                                default:
                                    UpdateLogText(" L1: RFU");
                                    break;
                            }
                            switch (bytes[1])
                            {
                                case 0x00:
                                    UpdateLogText(" L2: OK");
                                    break;
                                case 0x01:
                                    UpdateLogText(" L2: CARD DATA MISSING");
                                    break;
                                case 0x02:
                                    UpdateLogText(" L2: CAM FAILED");
                                    break;
                                case 0x03:
                                    UpdateLogText(" L2: STATUS BYTES");
                                    break;
                                case 0x04:
                                    UpdateLogText(" L2: PARSING ERROR");
                                    break;
                                case 0x05:
                                    UpdateLogText(" L2: MAX LIMIT EXCEEDED");
                                    break;
                                case 0x06:
                                    UpdateLogText(" L2: CARD DATA ERROR");
                                    break;
                                case 0x07:
                                    UpdateLogText(" L2: MAGSTRIPE NOT SUPPORTED");
                                    break;
                                case 0x08:
                                    UpdateLogText(" L2: NO PPSE");
                                    break;
                                case 0x09:
                                    UpdateLogText(" L2: PPSE FAULT");
                                    break;
                                case 0x0A:
                                    UpdateLogText(" L2: EMPTY CANDIDATE LIST");
                                    break;
                                case 0x0B:
                                    UpdateLogText(" L2: IDS READ ERROR");
                                    break;
                                case 0x0C:
                                    UpdateLogText(" L2: IDS WRITE ERROR");
                                    break;
                                case 0x0D:
                                    UpdateLogText(" L2: IDS DATA ERROR");
                                    break;
                                case 0x0E:
                                    UpdateLogText(" L2: IDS NO MATCHING AC");
                                    break;
                                case 0x0F:
                                    UpdateLogText(" L2: TERMINAL DATA ERROR");
                                    break;
                                default:
                                    UpdateLogText(" L2: RFU");
                                    break;
                            }
                            switch (bytes[2])
                            {
                                case 0x00:
                                    UpdateLogText(" L3: OK");
                                    break;
                                case 0x01:
                                    UpdateLogText(" L3: TIME OUT");
                                    break;
                                case 0x02:
                                    UpdateLogText(" L3: STOP");
                                    break;
                                case 0x03:
                                    UpdateLogText(" L3: AMOUNT NOT PRESENT");
                                    break;
                                default:
                                    UpdateLogText(" L3: RFU");
                                    break;
                            }

                            //截取discData的第四个字符和第五个字符
                            UpdateLogText(" SW12: " + item.Value.Substring(6, 4));
                            byte msg_on_err = bytes[5];
                            switch (msg_on_err)
                            {
                                case 0x17:
                                    UpdateLogText(" Msg On Error:  CARD READ OK");
                                    break;
                                case 0x21:
                                    UpdateLogText(" Msg On Error:  TRY AGAIN");
                                    break;
                                case 0x03:
                                    UpdateLogText(" Msg On Error:  APPROVED");
                                    break;
                                case 0x1A:
                                    UpdateLogText(" Msg On Error:  APPROVED – SIGN");
                                    break;
                                case 0x07:
                                    UpdateLogText(" Msg On Error:  DECLINED");
                                    break;
                                case 0x1C:
                                    UpdateLogText(" Msg On Error:  ERROR – OTHER CARD");
                                    break;
                                case 0x1D:
                                    UpdateLogText(" Msg On Error:  INSERT CARD");
                                    break;
                                case 0x20:
                                    UpdateLogText(" Msg On Error:  SEE PHONE");
                                    break;
                                case 0x1B:
                                    UpdateLogText(" Msg On Error:  AUTHORISING – PLEASE WAIT");
                                    break;
                                case 0x1E:
                                    UpdateLogText(" Msg On Error:  CLEAR DISPLAY");
                                    break;
                                case 0xFF:
                                    UpdateLogText(" Msg On Error:  N/A");
                                    break;
                                default:
                                    UpdateLogText(" Msg On Error:  N/A");
                                    break;
                            }
                        }
                        else
                        {
                            UpdateLogText($"{item.Key}: {item.Value}");
                        }
                    }
                }
                
                UpdateLogText("_____________________________________");
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
                byte[] hold_time = bytes.Skip(2).Take(3).ToArray();
                UpdateLogText("Hold Time: " + ByteArrayToHexString(hold_time, 0, 3));
                byte[] language_prefer = bytes.Skip(5).Take(8).ToArray();
                UpdateLogText("Language Preference: " + ByteArrayToHexString(language_prefer, 0, 8));
                UpdateLogText("_____________________________________");
            }
        }

        private void ShowKernelID(string kernelID)
        {
            if (kernelID != null)
            {
                UpdateLogText("Kernel ID: " + kernelID);
            }
            else
            {
                UpdateLogText("Kernel ID: 0200000000000000");
            }

            UpdateLogText("_____________________________________");
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
                        ShowDataRecord(data.Value);
                        break;
                    case "DiscData":
                        ShowDiscData(data.Value);
                        break;
                    case "UIRD":
                        ShowUIReq(data.Value);
                        break;
                    case "KernelId":
                        ShowKernelID(data.Value);
                        break;
                    default:
                        break;
                }
            }
        }

        //private void ProcessFromPOS(byte[] data)
        //{
        //    try
        //    {
        //        // 先将数据添加到缓冲区
        //        _serialBuffer.AddRange(data);

        //        using (var ms = new MemoryStream(_serialBuffer.ToArray()))
        //        {
        //            while (ms.Position < ms.Length)
        //            {
        //                try
        //                {
        //                    // 尝试解析一个带长度前缀的消息
        //                    Envelope envelope = Envelope.Parser.ParseDelimitedFrom(ms);

        //                    if (envelope != null)
        //                    {
        //                        // 处理消息
        //                        ProcessSingleEnvelope(envelope);

        //                        // 更新缓冲区：移除已处理的数据
        //                        byte[] remaining = new byte[ms.Length - ms.Position];
        //                        ms.Read(remaining, 0, remaining.Length);
        //                        _serialBuffer = new List<byte>(remaining);
        //                    }
        //                }
        //                catch (InvalidProtocolBufferException)
        //                {
        //                    // 如果解析失败，可能数据不完整，等待更多数据
        //                    MyLogManager.Log($"等待更多数据，当前缓冲区大小: {_serialBuffer.Count}");
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MyLogManager.Log($"ProcessFromPOS Exception: {ex.Message}");
        //        // 清空缓冲区，避免错误数据堆积
        //        _serialBuffer.Clear();
        //    }
        //}

        //// 处理单个消息
        //private void ProcessSingleEnvelope(Envelope envelope)
        //{
        //    bool transFlag = false, disconnectFlag = false;

        //    MyLogManager.Log($"ProcessFromPOS receive:{envelope.ToString()}");
        //    //LogFormattedProtobuf(envelope);

        //    MyLogManager.Log($"envelope.PayloadCase: {envelope.PayloadCase}");

        //    if (envelope.PayloadCase == Envelope.PayloadOneofCase.Signal)
        //    {
        //        SignalProtocol signalProtocol = envelope.Signal;
        //        MyLogManager.Log($"signalProtocol.Type: {signalProtocol.Type}");
        //        //UpdateLogText($"ProcessFromPOS receive signal: {signalProtocol.Type}");
        //        if (signalProtocol.Type == "OUT")
        //        {
        //            ParseOutSignal(signalProtocol.Data);
        //            transFlag = true;
        //            disconnectFlag = true;
        //            while (_queue.Count != 0)
        //            {
        //                _queue.TryDequeue(out SerialOperation op);
        //            }
        //        }
        //        else if (signalProtocol.Type == "MSG")
        //        {
        //            UpdateLogText("_____________________________________");
        //            ParseOutSignal(signalProtocol.Data);
        //            transFlag = true;
        //        }
        //        else if (signalProtocol.Type == "CONFIG")
        //        {
        //            foreach (var item in signalProtocol.Data)
        //            {
        //                if (item.Id == "CONF_NAME")
        //                {
        //                    if (SelectConfig != null && SelectConfig != "")
        //                    {
        //                        string fileName = SelectConfig + ".json";
        //                        string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        //                        string configDir = runDir + "Config\\Config\\";
        //                        if (Directory.Exists(configDir))
        //                        {
        //                            if (!File.Exists(configDir + fileName))
        //                            {
        //                                System.Windows.MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                                MyLogManager.Log($"Target Config{configDir + fileName} doesn't exist");
        //                            }
        //                            else
        //                            {
        //                                DownloadConfig(configDir + fileName, true);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            System.Windows.MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        System.Windows.MessageBox.Show("No Config Selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                    }
        //                }
        //            }
        //        }
        //        else if (signalProtocol.Type == "CAPK")
        //        {
        //            string fileName = "PAYPASS_CAPK.json";
        //            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        //            string configDir = runDir + "Config\\CAPK\\";
        //            MyLogManager.Log($"CAPK Dir:{configDir}");

        //            if (Directory.Exists(configDir))
        //            {
        //                MyLogManager.Log($"Target CAPK:{configDir + fileName}");
        //                if (!File.Exists(configDir + fileName))
        //                {
        //                    System.Windows.MessageBox.Show("Target CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                }
        //                else
        //                {
        //                    DownloadCAPK(configDir + fileName);
        //                }
        //            }
        //            else
        //            {
        //                System.Windows.MessageBox.Show("No CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }
        //        else if (signalProtocol.Type == "REVOCATION_PK")
        //        {
        //            string fileName = "PAYPASS_Revokey.json";
        //            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        //            string configDir = runDir + "Config\\Revocation_CAPK\\";
        //            MyLogManager.Log($"Revocation_CAPK Dir:{configDir}");

        //            if (Directory.Exists(configDir))
        //            {
        //                MyLogManager.Log($"Target Revocation_CAPK:{configDir + fileName}");
        //                if (!File.Exists(configDir + fileName))
        //                {
        //                    System.Windows.MessageBox.Show("Target Revocation_CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                }
        //                else
        //                {
        //                    DownloadRevokey(configDir + fileName);
        //                }
        //            }
        //            else
        //            {
        //                System.Windows.MessageBox.Show("No Revocation_CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }
        //        else if (signalProtocol.Type == "ACT_ACK")
        //        {
        //            transFlag = false;
        //            MyLogManager.Log("ACT_ACK");
        //            if (_queue.TryPeek(out SerialOperation operation) &&
        //                operation.OperationType.Equals("ACT"))
        //            {
        //                if (_queue.TryDequeue(out SerialOperation removed))
        //                {
        //                    MyLogManager.Log($"移除队首{operation.OperationType}操作成功");
        //                }
        //            }
        //        }
        //        else if (signalProtocol.Type == "CONFIG_ACK")
        //        {
        //            transFlag = false;
        //            MyLogManager.Log("收到CONFIG_ACK");
        //            if (_queue.TryPeek(out SerialOperation operation) &&
        //                operation.OperationType.Equals("CONFIG"))
        //            {
        //                if (_queue.TryDequeue(out SerialOperation removed))
        //                {
        //                    MyLogManager.Log($"移除队首{operation.OperationType}操作成功");
        //                }
        //            }
        //        }
        //        else if (signalProtocol.Type == "TEST_INFO_ACK")
        //        {
        //            transFlag = false;
        //            MyLogManager.Log("TEST_INFO_ACK");
        //            if (_queue.TryPeek(out SerialOperation operation) &&
        //                operation.OperationType.Equals("TEST_INFO"))
        //            {
        //                if (_queue.TryDequeue(out SerialOperation removed))
        //                {
        //                    MyLogManager.Log($"移除队首{operation.OperationType}操作成功");
        //                }
        //            }
        //        }
        //        else if (signalProtocol.Type == "DEK")
        //        {
        //            transFlag = true;
        //        }
        //        else
        //        {
        //            MyLogManager.Log("Unrecognized Signal Type");
        //        }

        //        if (transFlag)
        //        {
        //            TransformSignalToTestTool(signalProtocol, disconnectFlag);
        //        }
        //    }
        //    else
        //    {
        //        MyLogManager.Log("Unrecognized Protocol Type");
        //    }
        //}

        private void ProcessFromPOS(byte[] data)
        {
            try
            {
                Envelope envelope;

                // 方法1：先尝试使用 ParseDelimitedFrom（带长度前缀）
                using (var ms = new MemoryStream(data))
                {
                    try
                    {
                        envelope = Envelope.Parser.ParseDelimitedFrom(ms);
                        MyLogManager.Log($"使用带长度前缀解析成功，读取字节: {ms.Position}/{data.Length}");
                    }
                    catch (InvalidProtocolBufferException ex1)
                    {
                        MyLogManager.Log($"ParseDelimitedFrom 失败: {ex1.Message}");

                        // 方法2：如果失败，回退到 ParseFrom（不带长度前缀）
                        try
                        {
                            envelope = Envelope.Parser.ParseFrom(data);
                            MyLogManager.Log("使用标准解析成功");
                        }
                        catch (Exception ex2)
                        {
                            MyLogManager.Log($"所有解析方法都失败: {ex2.Message}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogManager.Log($"解析异常: {ex.Message}");
                        return;
                    }
                }

                bool transFlag = false, disconnectFlag = false;

                if (envelope != null)
                {
                    MyLogManager.Log($"ProcessFromPOS receive:{envelope.ToString()}\n ");
                    LogFormattedProtobuf(envelope);
                }
                else
                {
                    MyLogManager.Log("ProtocolBuf parse error");
                    return;
                }

                MyLogManager.Log($"envelope.PayloadCase: {envelope.PayloadCase}");

                if (envelope.PayloadCase == Envelope.PayloadOneofCase.Signal)
                {
                    SignalProtocol signalProtocol = envelope.Signal;
                    MyLogManager.Log($"signalProtocol.Type: {signalProtocol.Type}");
                    //UpdateLogText($"ProcessFromPOS receive signal: {signalProtocol.Type}");
                    if (signalProtocol.Type == "OUT")
                    {
                        ParseOutSignal(signalProtocol.Data);
                        transFlag = true;
                        disconnectFlag = true;
                        while (_queue.Count != 0)
                        {
                            _queue.TryDequeue(out SerialOperation op);
                        }
                    }
                    else if (signalProtocol.Type == "MSG")
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
                                            System.Windows.MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                            MyLogManager.Log($"Target Config{configDir + fileName} doesn't exist");
                                        }
                                        else
                                        {
                                            DownloadConfig(configDir + fileName, true);
                                        }
                                    }
                                    else
                                    {
                                        System.Windows.MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                                else
                                {
                                    System.Windows.MessageBox.Show("No Config Selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                System.Windows.MessageBox.Show("Target CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                DownloadCAPK(configDir + fileName);
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("No CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                System.Windows.MessageBox.Show("Target Revocation_CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                DownloadRevokey(configDir + fileName);
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("No Revocation_CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (signalProtocol.Type == "ACT_ACK")
                    {
                        transFlag = false;
                        MyLogManager.Log("ACT_ACK");
                        if (_queue.TryPeek(out SerialOperation operation) &&
                            operation.OperationType.Equals("ACT"))
                        {
                            if (_queue.TryDequeue(out SerialOperation removed))
                            {
                                MyLogManager.Log($"移除队首{operation.OperationType}操作成功");
                            }
                        }
                    }
                    else if (signalProtocol.Type == "CONFIG_ACK")
                    {
                        transFlag = false;
                        MyLogManager.Log("收到CONFIG_ACK");
                        if (_queue.TryPeek(out SerialOperation operation) &&
                            operation.OperationType.Equals("CONFIG"))
                        {
                            if (_queue.TryDequeue(out SerialOperation removed))
                            {
                                MyLogManager.Log($"移除队首{operation.OperationType}操作成功");
                            }
                        }
                    }
                    else if (signalProtocol.Type == "TEST_INFO_ACK")
                    {
                        transFlag = false;
                        MyLogManager.Log("TEST_INFO_ACK");
                        if (_queue.TryPeek(out SerialOperation operation) &&
                            operation.OperationType.Equals("TEST_INFO"))
                        {
                            if (_queue.TryDequeue(out SerialOperation removed))
                            {
                                MyLogManager.Log($"移除队首{operation.OperationType}操作成功");
                            }
                        }
                    }
                    else if (signalProtocol.Type == "DEK")
                    {
                        transFlag = true;
                    }
                    else
                    {
                        MyLogManager.Log("Unrecognized Signal Type");
                    }

                    if (transFlag)
                    {
                        TransformSignalToTestTool(signalProtocol, disconnectFlag);
                    }
                }
                else
                {
                    MyLogManager.Log("Unrecognized Protocol Type");
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"ProcessFromPOS Exception: {ex.Message}");
            }
        }

        //private void ProcessFromPOS(byte[] data)
        //{
        //    try
        //    {
        //        Envelope envelope = Envelope.Parser.ParseFrom(data);
        //        bool transFlag = false, disconnectFlag = false;

        //        if (envelope != null)
        //        {
        //            MyLogManager.Log($"ProcessFromPOS receive:{envelope.ToString()}\n ");
        //            LogFormattedProtobuf(envelope);
        //        }
        //        else
        //        {
        //            MyLogManager.Log("ProtocolBuf parse error");
        //            return;
        //        }

        //        MyLogManager.Log($"envelope.PayloadCase: {envelope.PayloadCase}");

        //        if (envelope.PayloadCase == Envelope.PayloadOneofCase.Signal)
        //        {
        //            SignalProtocol signalProtocol = envelope.Signal;
        //            MyLogManager.Log($"signalProtocol.Type: {signalProtocol.Type}");

        //            if (signalProtocol.Type == "OUT")
        //            {
        //                ParseOutSignal(signalProtocol.Data);
        //                transFlag = true;
        //                disconnectFlag = true;
        //            }
        //            else if (signalProtocol.Type == "MSG")
        //            {
        //                ParseOutSignal(signalProtocol.Data);
        //                transFlag = true;
        //            }
        //            else if (signalProtocol.Type == "CONFIG")
        //            {
        //                foreach (var item in signalProtocol.Data)
        //                {
        //                    if (item.Id == "CONF_NAME")
        //                    {
        //                        if (SelectConfig != null && SelectConfig != "")
        //                        {
        //                            string fileName = SelectConfig + ".json";
        //                            string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        //                            string configDir = runDir + "Config\\Config\\";
        //                            if (Directory.Exists(configDir))
        //                            {
        //                                if (!File.Exists(configDir + fileName))
        //                                {
        //                                    System.Windows.MessageBox.Show("Target Config doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                                    MyLogManager.Log($"Target Config{configDir + fileName} doesn't exist");
        //                                }
        //                                else
        //                                {
        //                                    DownloadConfig(configDir + fileName);
        //                                }
        //                            }
        //                            else
        //                            {
        //                                System.Windows.MessageBox.Show("No Config Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            System.Windows.MessageBox.Show("No Config Selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                        }
        //                    }
        //                }
        //            }
        //            else if (signalProtocol.Type == "CAPK")
        //            {
        //                string fileName = "PAYPASS_CAPK.json";
        //                string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        //                string configDir = runDir + "Config\\CAPK\\";
        //                MyLogManager.Log($"CAPK Dir:{configDir}");

        //                if (Directory.Exists(configDir))
        //                {
        //                    MyLogManager.Log($"Target CAPK:{configDir + fileName}");
        //                    if (!File.Exists(configDir + fileName))
        //                    {
        //                        System.Windows.MessageBox.Show("Target CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                    }
        //                    else
        //                    {
        //                        DownloadCAPK(configDir + fileName);
        //                    }
        //                }
        //                else
        //                {
        //                    System.Windows.MessageBox.Show("No CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                }
        //            }
        //            else if (signalProtocol.Type == "REVOCATION_PK")
        //            {
        //                string fileName = "PAYPASS_Revokey.json";
        //                string runDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        //                string configDir = runDir + "Config\\Revocation_CAPK\\";
        //                MyLogManager.Log($"Revocation_CAPK Dir:{configDir}");

        //                if (Directory.Exists(configDir))
        //                {
        //                    MyLogManager.Log($"Target Revocation_CAPK:{configDir + fileName}");
        //                    if (!File.Exists(configDir + fileName))
        //                    {
        //                        System.Windows.MessageBox.Show("Target Revocation_CAPK doesn't exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                    }
        //                    else
        //                    {
        //                        DownloadRevokey(configDir + fileName);
        //                    }
        //                }
        //                else
        //                {
        //                    System.Windows.MessageBox.Show("No Revocation_CAPK Dir to load,Please Check", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //                }
        //            }
        //            else if (signalProtocol.Type == "ACT_ACK")
        //            {
        //                MyLogManager.Log($"receiver act_ack signal:{_timer}");

        //                if (_timer != null)
        //                {
        //                    MyLogManager.Log($"stop resend timer");

        //                    _timer.Stop();
        //                    _timer.Dispose();
        //                }
        //            }
        //            else if (signalProtocol.Type == "CONFIG_ACK")
        //            {
        //                MyLogManager.Log($"receiver config_ack signal:{_configTimer}");

        //                if (_configTimer != null)
        //                {
        //                    MyLogManager.Log($"stop resend timer");

        //                    _configTimer.Stop();
        //                    _configTimer.Dispose();
        //                }
        //            }
        //            else if (signalProtocol.Type == "TEST_INFO_ACK")
        //            {
        //                transFlag = false;
        //            }
        //            else if (signalProtocol.Type == "DEK")
        //            {
        //                transFlag = true;
        //            }
        //            else
        //            {
        //                MyLogManager.Log("Unrecognized Signal Type");
        //            }

        //            if (_loopACTFlag)
        //            {
        //                OnLoopACTSend?.Invoke("Send");
        //            }
        //            else
        //            {
        //                if (transFlag)
        //                {
        //                    TransformSignalToTestTool(signalProtocol, disconnectFlag);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            MyLogManager.Log("Unrecognized Protocol Type");
        //        }
        //    }
        //    catch (Exception ex) 
        //    {
        //        MyLogManager.Log($"ProcessFromPOS Exception: {ex.Message}");
        //    }
        //}

        private void LogFormattedProtobuf(IMessage message)
        {
            try
            {
                // 1. 使用Protobuf的JsonFormatter转换为JSON
                string protoJson = JsonFormatter.Default.Format(message);

                // 2. 解析为JObject以便处理
                var jObj = JObject.Parse(protoJson);

                // 3. 解码所有Base64字段
                DecodeAllBinaryFields(jObj);

                // 4. 美化输出
                MyLogManager.Log("Formatted Data:\n" +
                               jObj.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Error formatting protobuf: {ex.Message}");
            }
        }

        private void DecodeAllBinaryFields(JToken token)
        {
            switch (token)
            {
                case JObject obj:
                    foreach (var property in obj.Properties())
                    {
                        if (property.Value.Type == JTokenType.String)
                        {
                            // 尝试解码Base64
                            property.Value = DecodeBase64String(property.Value.ToString());
                        }
                        else
                        {
                            DecodeAllBinaryFields(property.Value);
                        }
                    }
                    break;

                case JArray array:
                    foreach (var item in array)
                    {
                        DecodeAllBinaryFields(item);
                    }
                    break;
            }
        }

        private JToken DecodeBase64String(string base64Str)
        {
            try
            {
                if (base64Str.Length > 1024) return base64Str;

                byte[] bytes = Convert.FromBase64String(base64Str);

                // 如果是纯 ASCII 可打印字符
                if (bytes.All(b => b >= 32 && b <= 126))
                {
                    return $"ASCII: {Encoding.ASCII.GetString(bytes)}";
                }

                // 默认情况：转换为十六进制字符串
                return JToken.FromObject($"HEX: {BitConverter.ToString(bytes).Replace("-", "")}");
            }
            catch
            {
                return JToken.FromObject(base64Str); // 不是有效的Base64则保持原样
            }
        }

        // 清空队列
        public void ClearQueue()
        {
            try
            {
                int count = 0;
                while (_queue.TryDequeue(out _))
                {
                    count++;
                }
                MyLogManager.Log($"清空队列，移除了 {count} 个操作");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"清空队列失败: {ex.Message}");
            }
        }

        // 查看队列状态（调试用）
        private void LogQueueStatus()
        {
            MyLogManager.Log($"=== 队列状态 ===");
            MyLogManager.Log($"队列大小: {_queue.Count}");

            int index = 0;
            foreach (var operation in _queue)
            {
                TimeSpan elapsed = DateTime.Now - operation.EnqueueTime;
                MyLogManager.Log($"[{index}] {operation.OperationType} - " +
                               $"重试: {operation.RetryCount}, " +
                               $"等待: {elapsed.TotalSeconds:F1}秒");
                index++;
            }
            MyLogManager.Log("=================");
        }

        public void StartWorkerThread()
        {
            try
            {
                // 如果线程正在运行，先停止它
                if(_backGround?.IsAlive == true)
                {
                    return;
                }
                //StopWorkerThread();

                // 创建新的CancellationTokenSource
                _cts = new CancellationTokenSource();

                // 创建新线程
                _backGround = new Thread(() => WorkerLoop(_cts.Token));
                _backGround.IsBackground = true;  // 设置为后台线程
                _backGround.Start();

                // 等待线程真正启动
                if (_threadStartedEvent.Wait(TimeSpan.FromSeconds(3)))
                {
                    MyLogManager.Log("串口队列工作线程启动成功");
                }
                else
                {
                    MyLogManager.Log("串口队列工作线程启动超时");
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"启动工作线程失败: {ex.Message}");
                UpdateLogText($"启动后台线程失败: {ex.Message}");
            }
        }

        // 修改线程停止方法
        public void StopWorkerThread()
        {
            try
            {
                // 请求取消
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }

                // 等待线程结束
                if (_backGround != null && _backGround.IsAlive)
                {
                    // 优雅地等待线程结束
                    if (!_backGround.Join(TimeSpan.FromSeconds(3)))
                    {
                        MyLogManager.Log("线程未在3秒内结束，强制终止");
                        _backGround.Abort();  // 作为最后手段
                    }
                    _backGround = null;
                }

                _threadStartedEvent.Reset();
                MyLogManager.Log("工作线程已停止");
            }
            catch (ThreadAbortException)
            {
                MyLogManager.Log("线程被强制终止");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"停止工作线程失败: {ex.Message}");
                UpdateLogText($"停止后台线程失败: {ex.Message}");
            }
        }

        public bool GetWorkerThreadStatus()
        {
            if(_backGround != null)
            {
                return _backGround.IsAlive;
            }
            return false;
        }
    }
}
