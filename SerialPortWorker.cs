using MastercardHost.MessageProtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MastercardHost
{
    public class SerialPortWorker : IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly BlockingCollection<SerialRequest> _requestQueue;
        private readonly Thread _workerThread;
        private readonly CancellationTokenSource _cts;

        // 当前正在等待ACK的请求
        private SerialRequest _currentRequest;
        private readonly object _currentLock = new object();
        private System.Timers.Timer _ackTimer;


        // 重试相关
        private const int MAX_RETRY_COUNT = 3;
        private readonly TimeSpan DEFAULT_ACK_TIMEOUT = TimeSpan.FromSeconds(8);

        // 事件
        public event Action<byte[]> OnSerialDataReceived; // 收到非ACK数据
        public event Action<string, bool> OnRequestCompleted; // 请求完成（操作类型，是否成功）
        public event Action<string> OnSerialError; // 串口错误

        public SerialPortWorker(SerialPort serialPort)
        {
            _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
            _requestQueue = new BlockingCollection<SerialRequest>();
            _cts = new CancellationTokenSource();

            // 初始化ACK定时器（在主线程创建，确保线程安全）
            _ackTimer = new System.Timers.Timer();
            _ackTimer.Elapsed += OnAckTimeout;
            _ackTimer.AutoReset = false;

            // 注册串口数据接收
            _serialPort.DataReceived += OnSerialDataReceivedEvent;
            _serialPort.ErrorReceived += OnSerialErrorReceived;

            // 启动工作线程
            _workerThread = new Thread(WorkerLoop)
            {
                Name = "SerialPortWorker",
                IsBackground = true
            };
            _workerThread.Start();
            MyLogManager.Log($"串口工作线程已启动，端口: {_serialPort.PortName}");
        }

        private class SerialRequest
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
            public string OperationType { get; set; }  // "CONFIG", "ACT", "CAPK", etc.
            public byte[] Data { get; set; }
            public bool NeedsAck { get; set; } = true;  // 是否等待ACK
            public int RetryCount { get; set; } = 0;
            public TimeSpan AckTimeout { get; set; } = TimeSpan.FromSeconds(8);
            public DateTime EnqueueTime { get; set; } = DateTime.Now;
            public DateTime? SendTime { get; set; }
            public Action<bool> Callback { get; set; }  // 完成回调
            public bool IsBeingRetried { get; set; } = false; // 新增：标记是否正在重试
        }

        // 发送请求（立即返回）
        public void SendRequest(string operationType, byte[] data, bool needsAck = true,
                                  TimeSpan? ackTimeout = null, Action<bool> callback = null)
        {
            var request = new SerialRequest
            {
                OperationType = operationType,
                Data = data,
                NeedsAck = needsAck,
                AckTimeout = ackTimeout ?? GetDefaultTimeout(operationType),
                Callback = callback
            };

            _requestQueue.Add(request);
            MyLogManager.Log($"请求加入队列: {operationType} [ID:{request.Id}], 需要ACK: {needsAck}, 超时: {request.AckTimeout.TotalSeconds}秒");
        }

        public bool SendAndWait(string operationType, byte[] data, TimeSpan? timeout = null)
        {
            var completionSource = new ManualResetEventSlim(false);
            bool result = false;

            SendRequest(operationType, data, true, null, (success) =>
            {
                result = success;
                completionSource.Set();
            });

            // 等待完成或超时
            bool completed = completionSource.Wait(timeout ?? TimeSpan.FromSeconds(30));

            if (!completed)
            {
                MyLogManager.Log($"{operationType} 等待超时");
                return false;
            }

            return result;
        }

        private TimeSpan GetDefaultTimeout(string operationType)
        {
            switch (operationType)
            {
                case "ACT":
                    return TimeSpan.FromSeconds(5);
                case "CONFIG":
                    return TimeSpan.FromSeconds(10);
                case "CAPK":
                case "REVOCATION_PK":
                    return TimeSpan.FromSeconds(15);
                default:
                    return DEFAULT_ACK_TIMEOUT;
            }
        }
        // 工作线程主循环
        private void WorkerLoop()
        {
            MyLogManager.Log($"串口工作线程启动，线程ID: {Thread.CurrentThread.ManagedThreadId}");

            try
            {
                foreach (var request in _requestQueue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        // 关键修改：如果是重试请求，确保它真的是当前请求
                        if (request.IsBeingRetried)
                        {
                            lock (_currentLock)
                            {
                                // 只有当这个请求确实是当前请求时，才处理重试
                                if (_currentRequest != null && _currentRequest.Id == request.Id)
                                {
                                    ProcessRequest(request);
                                }
                                else
                                {
                                    // 如果不是当前请求，重新加入队列等待
                                    request.IsBeingRetried = false;
                                    _requestQueue.Add(request);
                                    MyLogManager.Log($"重试请求 [ID:{request.Id}] 不是当前请求，重新排队");
                                }
                            }
                        }
                        else
                        {
                            // 正常的新请求，等待前一个请求完成
                            WaitForPreviousRequest();
                            ProcessRequest(request);
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogManager.Log($"处理请求失败 [ID:{request.Id}]: {ex.Message}");
                        CompleteRequest(request, false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                MyLogManager.Log("串口工作线程正常退出");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"串口工作线程异常退出: {ex.Message}");
            }
        }

        private void WaitForPreviousRequest()
        {
            while (true)
            {
                lock (_currentLock)
                {
                    if (_currentRequest == null)
                        return;
                }

                // 等待当前请求完成
                Thread.Sleep(50);
            }
        }

        private void ProcessRequest(SerialRequest request)
        {
            lock (_currentLock)
            {
                if (_currentRequest != null && _currentRequest.Id != request.Id)
                {
                    // 不应该发生，但作为保护
                    MyLogManager.Log($"警告: 试图处理新请求 [ID:{request.Id}]，但当前已有请求 [ID:{_currentRequest?.Id}]");
                    return;
                }
                _currentRequest = request;
            }

            try
            {
                MyLogManager.Log($"开始处理请求: {request.OperationType} [ID:{request.Id}], 排队时间: {(DateTime.Now - request.EnqueueTime).TotalMilliseconds:F0}ms, 重试次数: {request.RetryCount}");
                // 发送数据
                request.SendTime = DateTime.Now;
                _serialPort.Write(request.Data, 0, request.Data.Length);

                MyLogManager.Log($"串口发送完成: {request.OperationType} [ID:{request.Id}], 数据长度: {request.Data.Length}");

                if (request.NeedsAck)
                {
                    // 启动ACK超时定时器
                    StartAckTimer(request);
                    MyLogManager.Log($"等待 {request.OperationType}_ACK... [ID:{request.Id}]");
                }
                else
                {
                    // 不需要ACK的操作立即完成
                    CompleteRequest(request, true);
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"发送失败 [ID:{request.Id}]: {ex.Message}");
                HandleSendFailure(request);
            }
        }

        private void StartAckTimer(SerialRequest request)
        {
            _ackTimer.Stop();
            _ackTimer.Interval = request.AckTimeout.TotalMilliseconds;
            _ackTimer.Start();

            MyLogManager.Log($"启动ACK超时定时器 [ID:{request.Id}]: {request.AckTimeout.TotalSeconds}秒");
        }
        private void OnSerialDataReceivedEvent(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort.BytesToRead > 0)
                {
                    byte[] buffer = new byte[_serialPort.BytesToRead];
                    int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        ProcessReceivedData(buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"接收串口数据异常: {ex.Message}");
            }
        }
        private void ProcessReceivedData(byte[] data)
        {
            try
            {
                var envelope = Envelope.Parser.ParseFrom(data);

                if (envelope.PayloadCase == Envelope.PayloadOneofCase.Signal)
                {
                    string signalType = envelope.Signal.Type;

                    // 检查是否是当前请求的ACK
                    SerialRequest currentRequest;
                    lock (_currentLock)
                    {
                        currentRequest = _currentRequest;
                    }

                    if (currentRequest != null && currentRequest.NeedsAck)
                    {
                        string expectedAck = $"{currentRequest.OperationType}_ACK";

                        if (signalType == expectedAck)
                        {
                            // 停止定时器
                            _ackTimer.Stop();

                            // 计算响应时间
                            TimeSpan responseTime = DateTime.Now - currentRequest.SendTime.Value;
                            MyLogManager.Log($"收到期待的ACK: {expectedAck} [ID:{currentRequest.Id}], 响应时间: {responseTime.TotalMilliseconds:F0}ms");

                            // 完成当前请求
                            CompleteRequest(currentRequest, true);
                            return;
                        }
                        else if (signalType == "ERROR" || signalType == "FAILURE")
                        {
                            // 收到错误响应
                            MyLogManager.Log($"收到错误响应: {signalType} [ID:{currentRequest.Id}]");
                            HandleSendFailure(currentRequest);
                            return;
                        }
                        else if (signalType == "OUT")
                        {
                            _requestQueue.
                        }
                    }

                    // 非ACK数据，转发给外部处理
                    OnSerialDataReceived?.Invoke(data);
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"解析串口数据异常: {ex.Message}");
            }
        }

        private void OnAckTimeout(object sender, ElapsedEventArgs e)
        {
            SerialRequest currentRequest;
            lock (_currentLock)
            {
                currentRequest = _currentRequest;
            }

            if (currentRequest != null && currentRequest.NeedsAck)
            {
                currentRequest.RetryCount++;

                if (currentRequest.RetryCount >= MAX_RETRY_COUNT)
                {
                    MyLogManager.Log($"{currentRequest.OperationType} [ID:{currentRequest.Id}] 重试{MAX_RETRY_COUNT}次仍失败，终止操作");
                    CompleteRequest(currentRequest, false);
                    OnSerialError?.Invoke($"{currentRequest.OperationType} 重试{MAX_RETRY_COUNT}次失败");
                }
                else
                {
                    MyLogManager.Log($"{currentRequest.OperationType} [ID:{currentRequest.Id}] ACK超时，第{currentRequest.RetryCount}次重试");

                    // 关键修改：标记为重试请求，不清除 _currentRequest
                    currentRequest.IsBeingRetried = true;

                    // 延迟后重新处理当前请求
                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        if (!_cts.IsCancellationRequested)
                        {
                            _requestQueue.Add(currentRequest);
                            MyLogManager.Log($"重试请求已加入队列 [ID:{currentRequest.Id}]");
                        }
                    });

                    // 注意：这里不设置 _currentRequest = null！
                    // 保持 _currentRequest 不变，直到重试完成或失败
                }
            }
        }

        private void HandleSendFailure(SerialRequest request)
        {
            request.RetryCount++;

            if (request.RetryCount >= MAX_RETRY_COUNT)
            {
                MyLogManager.Log($"{request.OperationType} [ID:{request.Id}] 发送失败{MAX_RETRY_COUNT}次");
                CompleteRequest(request, false);
            }
            else
            {
                MyLogManager.Log($"{request.OperationType} [ID:{request.Id}] 发送失败，第{request.RetryCount}次重试");

                // 标记为重试请求
                request.IsBeingRetried = true;

                // 等待后重新加入队列
                Task.Delay(2000).ContinueWith(_ =>
                {
                    if (!_cts.IsCancellationRequested)
                    {
                        _requestQueue.Add(request);
                        MyLogManager.Log($"发送失败重试请求已加入队列 [ID:{request.Id}]");
                    }
                });
            }
        }

        private void CompleteRequest(SerialRequest request, bool success)
        {
            try
            {
                // 停止定时器
                _ackTimer.Stop();

                // 清除当前请求
                lock (_currentLock)
                {
                    if (_currentRequest != null && _currentRequest.Id == request.Id)
                    {
                        _currentRequest = null;
                    }
                }

                // 记录结果
                string result = success ? "成功" : "失败";
                MyLogManager.Log($"请求完成: {request.OperationType} [ID:{request.Id}] - {result}, 总耗时: {(DateTime.Now - request.EnqueueTime).TotalMilliseconds:F0}ms");
                // 触发完成事件
                OnRequestCompleted?.Invoke(request.OperationType, success);

                // 执行回调
                request.Callback?.Invoke(success);
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"完成请求时异常: {ex.Message}");
            }
        }

        private void OnSerialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            string errorMsg = $"串口错误: {e.EventType}";
            MyLogManager.Log(errorMsg);

            // 当前请求失败
            SerialRequest currentRequest;
            lock (_currentLock)
            {
                currentRequest = _currentRequest;
            }

            if (currentRequest != null)
            {
                CompleteRequest(currentRequest, false);
            }

            OnSerialError?.Invoke(errorMsg);
        }

        /// <summary>
        /// 获取队列状态
        /// </summary>
        public SerialQueueStatus GetStatus()
        {
            lock (_currentLock)
            {
                return new SerialQueueStatus
                {
                    QueueCount = _requestQueue.Count,
                    CurrentOperation = _currentRequest?.OperationType,
                    CurrentRequestId = _currentRequest?.Id,
                    CurrentRetryCount = _currentRequest?.RetryCount ?? 0,
                    IsProcessing = _currentRequest != null
                };
            }
        }

        public class SerialQueueStatus
        {
            public int QueueCount { get; set; }
            public string CurrentOperation { get; set; }
            public string CurrentRequestId { get; set; }
            public int CurrentRetryCount { get; set; }
            public bool IsProcessing { get; set; }

            public override string ToString()
            {
                return $"队列: {QueueCount}, 当前: {CurrentOperation ?? "无"} [ID:{CurrentRequestId}], 重试: {CurrentRetryCount}";
            }
        }

        public void Dispose()
        {
            MyLogManager.Log("正在关闭串口工作线程...");

            _cts?.Cancel();
            _requestQueue?.CompleteAdding();

            _ackTimer?.Stop();
            _ackTimer?.Dispose();

            if (_workerThread != null && _workerThread.IsAlive)
            {
                if (!_workerThread.Join(5000))
                {
                    MyLogManager.Log("串口工作线程未在5秒内结束，强制终止");
                    _workerThread.Interrupt();
                }
            }

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= OnSerialDataReceivedEvent;
                _serialPort.ErrorReceived -= OnSerialErrorReceived;
            }

            _requestQueue?.Dispose();
            _cts?.Dispose();

            MyLogManager.Log("串口工作线程已关闭");
        }
    }
}
