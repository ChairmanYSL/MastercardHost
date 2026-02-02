using MastercardHost.MessageProtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MastercardHost
{
    public class SerialCommunicationManager : IDisposable
    {
        private SerialPort _serialPort;
        private readonly BlockingCollection<SerialOperation> _operationQueue;
        private readonly CancellationTokenSource _cts;
        private Task _processingTask;
        private readonly SemaphoreSlim _serialLock = new SemaphoreSlim(1, 1);
        private CommunicationState _currentState = CommunicationState.Idle;
        private readonly object _stateLock = new object();

        // 事件
        public event Action<byte[]> OnSerialDataReceived;
        public event Action<string, bool> OnOperationCompleted; // operationType, success
        public event Action<string> OnSerialError;

        // 当前正在等待ACK的操作
        private SerialOperation _pendingOperation;
        private System.Timers.Timer _ackTimeoutTimer;

        public SerialCommunicationManager()
        {
            _operationQueue = new BlockingCollection<SerialOperation>();
            _cts = new CancellationTokenSource();

            // 启动处理线程
            _processingTask = Task.Run(ProcessOperations);

            // 初始化ACK超时定时器
            _ackTimeoutTimer = new System.Timers.Timer();
            _ackTimeoutTimer.Elapsed += OnAckTimeout;
            _ackTimeoutTimer.AutoReset = false;
        }

        public void InitializeSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            lock (_stateLock)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }

                _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    WriteTimeout = 5000,
                    ReadTimeout = 5000
                };

                _serialPort.DataReceived += HandleSerialDataReceived;
                _serialPort.ErrorReceived += HandleSerialError;
            }
        }

        public bool OpenSerialPort()
        {
            try
            {
                lock (_stateLock)
                {
                    if (_serialPort != null && !_serialPort.IsOpen)
                    {
                        _serialPort.Open();
                        SetState(CommunicationState.Idle);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                OnSerialError?.Invoke($"打开串口失败: {ex.Message}");
                return false;
            }
            return false;
        }

        public void CloseSerialPort()
        {
            lock (_stateLock)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                SetState(CommunicationState.Disconnected);
            }
        }

        // 添加操作到队列（立即返回，不等待完成）
        public void EnqueueOperation(string operationType, byte[] data)
        {
            var operation = new SerialOperation
            {
                OperationType = operationType,
                Data = data,
                EnqueueTime = DateTime.Now,
                CompletionSource = new TaskCompletionSource<bool>()
            };

            _operationQueue.Add(operation);
        }

        // 添加操作并等待完成（带超时）
        public async Task<bool> EnqueueAndWaitOperation(string operationType, byte[] data, TimeSpan timeout)
        {
            MyLogManager.Log("start EnqueueAndWaitOperation");
            MyLogManager.Log($"input operationType: {operationType}");
            MyLogManager.Log($"input data: {data.ToString()}");
            MyLogManager.Log($"input timeout: {timeout}");

            var operation = new SerialOperation
            {
                OperationType = operationType,
                Data = data,
                EnqueueTime = DateTime.Now,
                CompletionSource = new TaskCompletionSource<bool>()
            };

            _operationQueue.Add(operation);

            try
            {
                var timeoutTask = Task.Delay(timeout, _cts.Token);
                var completedTask = await Task.WhenAny(operation.CompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    return false; // 超时
                }

                return await operation.CompletionSource.Task;
            }
            catch
            {
                return false;
            }
        }

        private async Task ProcessOperations()
        {
            foreach (var operation in _operationQueue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    await ProcessSingleOperation(operation);
                }
                catch (Exception ex)
                {
                    MyLogManager.Log($"处理串口操作失败: {ex.Message}");
                    operation.CompletionSource?.TrySetResult(false);
                }
            }
        }

        private async Task ProcessSingleOperation(SerialOperation operation)
        {
            // 检查当前状态是否允许执行
            if (_currentState == CommunicationState.SerialError)
            {
                MyLogManager.Log($"串口错误状态，跳过操作: {operation.OperationType}");
                operation.CompletionSource?.TrySetResult(false);
                return;
            }

            // 检查是否需要等待前一个操作完成
            while (_pendingOperation != null)
            {
                await Task.Delay(100);
            }

            try
            {
                // 锁定串口进行发送
                await _serialLock.WaitAsync();

                try
                {
                    // 发送数据
                    _serialPort.Write(operation.Data, 0, operation.Data.Length);
                    MyLogManager.Log($"串口发送完成: {operation.OperationType}, 长度: {operation.Data.Length}");

                    // 如果需要等待ACK
                    if (operation.OperationType == "CONFIG" || operation.OperationType == "ACT")
                    {
                        _pendingOperation = operation;
                        StartAckTimeoutTimer();

                        // 更新状态
                        SetState(operation.OperationType == "CONFIG"
                            ? CommunicationState.WaitingForConfigAck
                            : CommunicationState.WaitingForActAck);
                    }
                    else
                    {
                        // 不需要ACK的操作立即完成
                        operation.CompletionSource?.TrySetResult(true);
                        OnOperationCompleted?.Invoke(operation.OperationType, true);
                    }
                }
                finally
                {
                    _serialLock.Release();
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"串口发送失败: {ex.Message}");
                HandleSendFailure(operation);
            }
        }

        private void HandleSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    _serialPort.Read(buffer, 0, bytesToRead);

                    // 处理接收到的数据
                    ProcessReceivedData(buffer);
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
                // 解析数据包
                var envelope = Envelope.Parser.ParseFrom(data);

                if (envelope.PayloadCase == Envelope.PayloadOneofCase.Signal)
                {
                    string signalType = envelope.Signal.Type;

                    // 检查是否是期待的ACK
                    if (_pendingOperation != null)
                    {
                        if ((_pendingOperation.OperationType == "CONFIG" && signalType == "CONFIG_ACK") ||
                            (_pendingOperation.OperationType == "ACT" && signalType == "ACT_ACK"))
                        {
                            // 停止超时定时器
                            _ackTimeoutTimer.Stop();

                            // 完成操作
                            _pendingOperation.CompletionSource?.TrySetResult(true);
                            OnOperationCompleted?.Invoke(_pendingOperation.OperationType, true);

                            // 重置状态
                            SetState(CommunicationState.Idle);
                            _pendingOperation = null;

                            MyLogManager.Log($"收到期待的ACK: {signalType}");
                            return;
                        }
                    }
                }

                // 其他数据转发给上层处理
                OnSerialDataReceived?.Invoke(data);
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"解析串口数据异常: {ex.Message}");
            }
        }

        private void HandleSerialError(object sender, SerialErrorReceivedEventArgs e)
        {
            string errorMsg = $"串口错误: {e.EventType}";
            MyLogManager.Log(errorMsg);

            SetState(CommunicationState.SerialError);
            OnSerialError?.Invoke(errorMsg);

            // 清理pending操作
            if (_pendingOperation != null)
            {
                _pendingOperation.CompletionSource?.TrySetResult(false);
                _pendingOperation = null;
            }

            // 触发错误恢复流程
            _ = RecoverFromSerialError();
        }

        private async Task RecoverFromSerialError()
        {
            MyLogManager.Log("开始串口错误恢复...");

            // 关闭串口
            CloseSerialPort();

            // 等待一会
            await Task.Delay(1000);

            // 尝试重新打开
            if (OpenSerialPort())
            {
                SetState(CommunicationState.Idle);
                MyLogManager.Log("串口恢复成功");
            }
            else
            {
                MyLogManager.Log("串口恢复失败");
            }
        }

        private void HandleSendFailure(SerialOperation operation)
        {
            operation.RetryCount++;

            if (operation.RetryCount >= 3)
            {
                MyLogManager.Log($"操作 {operation.OperationType} 重试3次仍失败");
                operation.CompletionSource?.TrySetResult(false);
                OnOperationCompleted?.Invoke(operation.OperationType, false);

                // 触发串口错误状态
                SetState(CommunicationState.SerialError);
                OnSerialError?.Invoke($"发送失败超过3次: {operation.OperationType}");
            }
            else
            {
                MyLogManager.Log($"操作 {operation.OperationType} 发送失败，准备重试 (第{operation.RetryCount}次)");

                // 延迟后重新加入队列
                Task.Delay(1000).ContinueWith(_ =>
                {
                    _operationQueue.Add(operation);
                });
            }
        }

        private void StartAckTimeoutTimer()
        {
            _ackTimeoutTimer.Stop();
            _ackTimeoutTimer.Interval = 8000; // 8秒超时
            _ackTimeoutTimer.Start();
        }

        private void OnAckTimeout(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_pendingOperation != null)
            {
                MyLogManager.Log($"等待 {_pendingOperation.OperationType}_ACK 超时");

                // 处理超时
                HandleSendFailure(_pendingOperation);
                _pendingOperation = null;
                SetState(CommunicationState.Idle);
            }
        }

        private void SetState(CommunicationState newState)
        {
            lock (_stateLock)
            {
                _currentState = newState;
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _operationQueue?.CompleteAdding();

            if (_serialPort != null)
            {
                _serialPort.DataReceived -= HandleSerialDataReceived;
                _serialPort.ErrorReceived -= HandleSerialError;
                _serialPort.Close();
                _serialPort.Dispose();
            }

            _processingTask?.Wait(5000);
            _ackTimeoutTimer?.Dispose();
            _operationQueue?.Dispose();
            _cts?.Dispose();
        }
    }
}
