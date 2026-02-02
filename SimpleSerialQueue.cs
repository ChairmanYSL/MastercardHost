using Google.Protobuf;
using MastercardHost.MessageProtos;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MastercardHost
{
    public class SimpleSerialQueue : IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly BlockingCollection<SerialRequest> _queue;
        private readonly Thread _workerThread;
        private readonly CancellationTokenSource _cts;

        // ACK等待器字典
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _ackWaiters;

        // 接收缓冲区
        private MemoryStream _receiveBuffer;
        private readonly object _bufferLock = new object();

        // 当前正在处理的请求
        private SerialRequest _currentRequest;
        private readonly object _currentLock = new object();
        private System.Timers.Timer _ackTimer;

        // 配置
        private const int MAX_RETRY_COUNT = 3;
        private const int BUFFER_SIZE = 4096;

        public event Action<byte[]> OnSerialDataReceived;
        public event Action<string, bool> OnRequestCompleted;
        public event Action<string> OnSerialError;

        public SimpleSerialQueue(SerialPort serialPort)
        {
            _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
            _queue = new BlockingCollection<SerialRequest>(new ConcurrentQueue<SerialRequest>());
            _cts = new CancellationTokenSource();
            _ackWaiters = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
            _receiveBuffer = new MemoryStream(BUFFER_SIZE);

            // 初始化ACK定时器（备用）
            _ackTimer = new System.Timers.Timer();
            _ackTimer.Elapsed += OnAckTimeout;
            _ackTimer.AutoReset = false;

            // 注册串口事件
            _serialPort.DataReceived += OnSerialDataReceivedEvent;
            _serialPort.ErrorReceived += OnSerialErrorReceived;

            // 启动工作线程
            _workerThread = new Thread(WorkerLoop)
            {
                Name = "SerialQueueWorker",
                IsBackground = true
            };
            _workerThread.Start();

            MyLogManager.Log($"串口队列已启动，端口: {_serialPort.PortName}");
        }

        private class SerialRequest
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
            public string Type { get; set; }
            public byte[] Data { get; set; }
            public bool NeedsAck { get; set; } = true;
            public int RetryCount { get; set; } = 0;
            public DateTime EnqueueTime { get; set; } = DateTime.Now;
            public DateTime? SendTime { get; set; }
            public Action<bool> Callback { get; set; }
        }

        /// <summary>
        /// 添加请求到队列
        /// </summary>
        public void Enqueue(string type, byte[] data, bool needsAck = true, Action<bool> callback = null)
        {
            var request = new SerialRequest
            {
                Type = type,
                Data = data,
                NeedsAck = needsAck,
                Callback = callback
            };

            _queue.Add(request);
            MyLogManager.Log($"请求入队: {type} [ID:{request.Id}], 需要ACK: {needsAck}, 数据长度: {data.Length} bytes, 队列长度: {_queue.Count}");
        }

        private async void WorkerLoop()
        {
            MyLogManager.Log($"串口队列工作线程启动");

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var request = _queue.Take(_cts.Token);
                        await ProcessSingleRequestAsync(request);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        MyLogManager.Log($"处理队列异常: {ex.Message}");
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"工作线程异常退出: {ex.Message}");
            }

            MyLogManager.Log("串口队列工作线程结束");
        }

        private async Task ProcessSingleRequestAsync(SerialRequest request)
        {
            try
            {
                lock (_currentLock)
                {
                    _currentRequest = request;
                }

                MyLogManager.Log($"开始处理请求: {request.Type} [ID:{request.Id}], 数据长度: {request.Data.Length} bytes");

                request.RetryCount = 0;

                while (request.RetryCount < MAX_RETRY_COUNT)
                {
                    try
                    {
                        request.SendTime = DateTime.Now;

                        // 记录发送前的状态
                        MyLogManager.Log($"准备发送数据 [ID:{request.Id}], 缓冲区状态: {_serialPort.BytesToWrite} bytes pending");

                        _serialPort.Write(request.Data, 0, request.Data.Length);

                        int pending = _serialPort.BytesToWrite;
                        MyLogManager.Log($"串口发送完成: {request.Type} [ID:{request.Id}], 发送后缓冲区: {pending} bytes pending");

                        if (request.NeedsAck)
                        {
                            // 使用异步等待ACK
                            bool ackReceived = await WaitForAckAsync(request);

                            if (ackReceived)
                            {
                                CompleteRequest(request, true);
                                return;
                            }
                            else
                            {
                                request.RetryCount++;

                                if (request.RetryCount < MAX_RETRY_COUNT)
                                {
                                    MyLogManager.Log($"{request.Type} [ID:{request.Id}] 第{request.RetryCount}次重试");
                                    await Task.Delay(1000);
                                }
                            }
                        }
                        else
                        {
                            CompleteRequest(request, true);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogManager.Log($"发送失败 [ID:{request.Id}]: {ex.Message}");
                        request.RetryCount++;

                        if (request.RetryCount < MAX_RETRY_COUNT)
                        {
                            MyLogManager.Log($"{request.Type} [ID:{request.Id}] 发送失败重试");
                            await Task.Delay(2000);
                        }
                    }
                }

                MyLogManager.Log($"{request.Type} [ID:{request.Id}] 重试失败");
                CompleteRequest(request, false);
            }
            finally
            {
                lock (_currentLock)
                {
                    if (_currentRequest?.Id == request.Id)
                    {
                        _currentRequest = null;
                    }
                }
            }
        }

        private async Task<bool> WaitForAckAsync(SerialRequest request)
        {
            var timeout = GetAckTimeout(request.Type);

            // 创建唯一的等待器Key
            string waiterKey = $"{request.Type}_{request.Id}";
            var completionSource = new TaskCompletionSource<bool>();

            // 注册ACK等待器
            _ackWaiters[waiterKey] = completionSource;

            try
            {
                MyLogManager.Log($"开始等待 {request.Type}_ACK [ID:{request.Id}], 超时: {timeout.TotalSeconds}秒");

                // 创建超时任务
                var timeoutTask = Task.Delay(timeout);

                // 等待第一个完成的任务
                var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    MyLogManager.Log($"{request.Type}_ACK 超时 [ID:{request.Id}]");
                    return false;
                }

                bool result = await completionSource.Task;
                MyLogManager.Log($"{request.Type}_ACK 收到 [ID:{request.Id}]");
                return result;
            }
            finally
            {
                // 清理等待器
                _ackWaiters.TryRemove(waiterKey, out _);
            }
        }

        private TimeSpan GetAckTimeout(string requestType)
        {
            switch (requestType)
            {
                case "ACT":
                    return TimeSpan.FromSeconds(3);
                case "CONFIG":
                    return TimeSpan.FromSeconds(5);
                case "CAPK":
                case "REVOCATION_PK":
                    return TimeSpan.FromSeconds(15);
                default:
                    return TimeSpan.FromSeconds(3);
            }
        }

        private void OnSerialDataReceivedEvent(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort.BytesToRead > 0 && _serialPort.IsOpen)
                {
                    byte[] buffer = new byte[_serialPort.BytesToRead];
                    int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);

                    MyLogManager.Log($"接收串口数据: {bytesRead} bytes");

                    if (bytesRead > 0)
                    {
                        // 累积数据到缓冲区
                        lock (_bufferLock)
                        {
                            _receiveBuffer.Write(buffer, 0, bytesRead);
                        }

                        // 处理缓冲区，解析出完整消息
                        ProcessReceiveBuffer();
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"接收串口数据异常: {ex.Message}");
            }
        }

        private void ProcessReceiveBuffer()
        {
            lock (_bufferLock)
            {
                if (_receiveBuffer.Length == 0) return;

                _receiveBuffer.Position = 0;
                byte[] bufferData = _receiveBuffer.ToArray();

                try
                {
                    // 尝试解析消息
                    Envelope envelope = null;

                    // 方式1：直接解析
                    try
                    {
                        envelope = Envelope.Parser.ParseFrom(bufferData);
                        MyLogManager.Log($"√ 直接解析成功，缓冲区长度: {bufferData.Length}");
                    }
                    catch (InvalidProtocolBufferException ex1)
                    {
                        MyLogManager.Log($"直接解析失败: {ex1.Message}");

                        // 方式2：跳过第一个字节（可能是varint长度前缀）
                        if (bufferData.Length > 1)
                        {
                            try
                            {
                                envelope = Envelope.Parser.ParseFrom(bufferData, 1, bufferData.Length - 1);
                                MyLogManager.Log($"√ 跳过第一个字节解析成功");
                            }
                            catch (InvalidProtocolBufferException ex2)
                            {
                                MyLogManager.Log($"跳过字节解析失败: {ex2.Message}");

                                // 方式3：尝试在数据中查找有效消息
                                envelope = FindMessageInBuffer(bufferData);
                                if (envelope != null)
                                {
                                    MyLogManager.Log($"√ 在缓冲区中查找到消息");
                                }
                            }
                        }
                    }

                    if (envelope != null)
                    {
                        // 处理消息
                        HandleReceivedMessage(envelope);

                        // 计算消息长度（用于清理缓冲区）
                        int messageLength = envelope.ToByteArray().Length;

                        // 如果消息是以标准方式解析的，清理缓冲区
                        _receiveBuffer.SetLength(0);

                        // 检查是否有剩余数据
                        int totalLength = bufferData.Length;
                        int processedLength = messageLength;

                        // 如果跳过了一个字节，需要调整处理长度
                        if (envelope.ToByteArray().Length == bufferData.Length - 1)
                        {
                            processedLength = messageLength + 1; // 包含跳过的字节
                        }

                        // 如果有未处理的数据，放回缓冲区
                        if (processedLength < totalLength)
                        {
                            int remaining = totalLength - processedLength;
                            byte[] remainingData = new byte[remaining];
                            Array.Copy(bufferData, processedLength, remainingData, 0, remaining);

                            _receiveBuffer.Write(remainingData, 0, remaining);
                            MyLogManager.Log($"保留 {remaining} 字节未处理数据");
                        }
                    }
                    else
                    {
                        MyLogManager.Log($"无法解析消息，清空缓冲区");
                        _receiveBuffer.SetLength(0);
                    }
                }
                catch (Exception ex)
                {
                    MyLogManager.Log($"处理缓冲区异常: {ex.Message}");
                    _receiveBuffer.SetLength(0);
                }
            }
        }

        private Envelope FindMessageInBuffer(byte[] bufferData)
        {
            // 尝试不同的起始位置查找有效消息
            for (int start = 0; start < Math.Min(bufferData.Length, 10); start++)
            {
                try
                {
                    var envelope = Envelope.Parser.ParseFrom(bufferData, start, bufferData.Length - start);
                    int messageLength = envelope.ToByteArray().Length;

                    MyLogManager.Log($"在偏移 {start} 处找到消息，长度: {messageLength}");
                    return envelope;
                }
                catch
                {
                    // 继续尝试
                }
            }

            return null;
        }

        private void HandleReceivedMessage(Envelope envelope)
        {
            try
            {
                if (envelope.PayloadCase == Envelope.PayloadOneofCase.Signal)
                {
                    string signalType = envelope.Signal.Type;
                    MyLogManager.Log($"收到信号: {signalType}");

                    // 特殊处理：收到OUT信号时清空请求队列
                    if (signalType == "OUT")
                    {
                        ClearAllQueues();
                    }

                    // 1. 检查是否是ACK信号
                    if (signalType.EndsWith("_ACK"))
                    {
                        string requestType = signalType.Substring(0, signalType.Length - 4);

                        // 查找并通知所有等待此类型ACK的请求
                        var matchingKeys = _ackWaiters.Keys
                            .Where(k => k.StartsWith(requestType + "_"))
                            .ToList();

                        if (matchingKeys.Count > 0)
                        {
                            MyLogManager.Log($"找到 {matchingKeys.Count} 个等待 {signalType} 的请求");

                            foreach (var key in matchingKeys)
                            {
                                if (_ackWaiters.TryRemove(key, out var completionSource))
                                {
                                    MyLogManager.Log($"通知ACK等待器: {key}");
                                    completionSource.TrySetResult(true);
                                }
                            }
                        }
                        else
                        {
                            MyLogManager.Log($"没有找到等待 {signalType} 的请求");
                        }
                    }

                    // 2. 触发事件让外部处理（如ProcessFromPOS）
                    byte[] data = envelope.ToByteArray();
                    OnSerialDataReceived?.Invoke(data);

                    // 3. 记录日志
                    MyLogManager.Log($"信号处理完成: {signalType}");
                }
                else
                {
                    MyLogManager.Log($"收到POS数据");
                    byte[] data = envelope.ToByteArray();
                    OnSerialDataReceived?.Invoke(data);
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"处理消息异常: {ex.Message}");
            }
        }

        public void ClearAllQueues()
        {
            try
            {
                MyLogManager.Log("收到OUT信号，开始清空所有队列...");

                int clearedCount = 0;

                // 1. 清空请求队列
                while (_queue.Count > 0)
                {
                    if (_queue.TryTake(out var request))
                    {
                        MyLogManager.Log($"从请求队列中移除: {request.Type} [ID:{request.Id}]");
                        clearedCount++;

                        // 执行回调（失败）
                        request.Callback?.Invoke(false);
                    }
                }

                // 2. 取消所有ACK等待器
                foreach (var key in _ackWaiters.Keys.ToList())
                {
                    if (_ackWaiters.TryRemove(key, out var completionSource))
                    {
                        MyLogManager.Log($"取消ACK等待器: {key}");
                        completionSource.TrySetResult(false);
                        clearedCount++;
                    }
                }

                // 3. 清空接收缓冲区
                lock (_bufferLock)
                {
                    _receiveBuffer.SetLength(0);
                }

                // 4. 重置当前请求
                lock (_currentLock)
                {
                    if (_currentRequest != null)
                    {
                        MyLogManager.Log($"取消当前处理中的请求: {_currentRequest.Type} [ID:{_currentRequest.Id}]");
                        _currentRequest = null;
                    }
                }

                MyLogManager.Log($"清空完成，共清理了 {clearedCount} 个项目");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"清空队列时异常: {ex.Message}");
            }
        }

        private void OnAckTimeout(object sender, ElapsedEventArgs e)
        {
            // 这个方法现在不需要了，因为我们在 WaitForAckAsync 中使用 TaskCompletionSource
        }

        private void CompleteRequest(SerialRequest request, bool success)
        {
            try
            {
                string result = success ? "成功" : "失败";
                MyLogManager.Log($"请求完成: {request.Type} [ID:{request.Id}] - {result}, 总耗时: {(DateTime.Now - request.EnqueueTime).TotalMilliseconds:F0}ms");

                // 触发完成事件
                OnRequestCompleted?.Invoke(request.Type, success);

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
            lock (_currentLock)
            {
                if (_currentRequest != null)
                {
                    CompleteRequest(_currentRequest, false);
                    _currentRequest = null;
                }
            }

            OnSerialError?.Invoke(errorMsg);
        }

        /// <summary>
        /// 获取队列状态
        /// </summary>
        public QueueStatus GetStatus()
        {
            return new QueueStatus
            {
                QueueCount = _queue.Count,
                CurrentRequest = _currentRequest?.Type,
                CurrentRequestId = _currentRequest?.Id,
                CurrentRetryCount = _currentRequest?.RetryCount ?? 0,
                AckWaiterCount = _ackWaiters.Count,
                BufferSize = _receiveBuffer.Length
            };
        }

        public class QueueStatus
        {
            public int QueueCount { get; set; }
            public string CurrentRequest { get; set; }
            public string CurrentRequestId { get; set; }
            public int CurrentRetryCount { get; set; }
            public int AckWaiterCount { get; set; }
            public long BufferSize { get; set; }

            public override string ToString()
            {
                return $"队列: {QueueCount}, 当前: {CurrentRequest ?? "无"} [ID:{CurrentRequestId}], " +
                       $"重试: {CurrentRetryCount}, ACK等待: {AckWaiterCount}, 缓冲区: {BufferSize}字节";
            }
        }

        public void Dispose()
        {
            MyLogManager.Log("正在关闭串口队列...");

            _cts?.Cancel();
            _queue?.CompleteAdding();

            // 取消所有ACK等待
            foreach (var waiter in _ackWaiters.Values)
            {
                waiter.TrySetCanceled();
            }
            _ackWaiters.Clear();

            _ackTimer?.Stop();
            _ackTimer?.Dispose();

            if (_workerThread != null && _workerThread.IsAlive)
            {
                if (!_workerThread.Join(5000))
                {
                    MyLogManager.Log("工作线程未在5秒内结束");
                }
            }

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= OnSerialDataReceivedEvent;
                _serialPort.ErrorReceived -= OnSerialErrorReceived;

                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }

            lock (_bufferLock)
            {
                _receiveBuffer?.Dispose();
            }

            _queue?.Dispose();
            _cts?.Dispose();

            MyLogManager.Log("串口队列已关闭");
        }
    }
}