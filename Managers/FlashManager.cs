using ASRClientCore.Models.Interfaces;
using static ASRClientCore.Models.Enums.AsrResponseStatus.ResponseStatus;
using static ASRClientCore.Models.Enums.AsrResponseStatus;
using ASRClientCore.Models.Exceptions;
using System;
using ASRClientCore.Models.Enums;

namespace ASRClientCore.DeviceManager
{
    public class FlashManager : IDisposable
    {
        private readonly IRequestManager manager;
        private readonly IAsrProtocolHandler handler;
        private readonly object _lock = new object();
        private readonly Task keepDeviceAliveTask;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private const uint MaxReadSize = 0x1000000;
        public uint Timeout
        {
            get => handler.Timeout;
            set => handler.Timeout = value;
        }
        public uint PerBlockSize { get; set { field = value < MaxReadSize ? value : MaxReadSize; } } = MaxReadSize;
        public event Action<string>? Log;
        public event Action<string>? UpdatePercentage;
        public FlashManager(IRequestManager manager)
        {
            this.manager = manager;
            this.handler = manager.Handler;
            manager.SendGetInformationRequest();
            keepDeviceAliveTask = Task.Run(() => KeepDeviceAlive(cts.Token));
        }
        public void Dispose() 
        { 
            cts.Cancel();
            try
            {
                keepDeviceAliveTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { }
            GC.SuppressFinalize(this);
        }
        public void ReadPartition(string partName,Stream outputStream)
        {
            ResponseStatus response;
            if (outputStream == null || !outputStream.CanWrite)
            {
                throw new ArgumentException("output stream must be writable");
            }
            lock (_lock)
            {
                if(Okey != (response = manager.SendReadPartitionRequest(partName,out var size)) || size == 0) throw new BadResponseException(response);
                Log?.Invoke($"target partition : {partName}, size : {size / 1024 / 1024}MB");
                byte[] buffer = new byte[MaxReadSize];
                for (ulong i = 0;i < size;)
                {
                    uint readSize = (uint)Math.Min(size - i, PerBlockSize);
                    if (0 == handler.Read(buffer, 0, (int)readSize)) throw new BadResponseException(ReadError,handler.LastErrorCode);
                    outputStream.Write(buffer, 0, (int)readSize);
                    i += readSize;
                    UpdatePercentage?.Invoke($"{partName} {(double)i * 100 / size:F2}%");
                }
            }
        }
        private void KeepDeviceAlive(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(10000);
                lock (_lock)
                {
                    token.ThrowIfCancellationRequested();
                    manager.SendGetInformationRequest();
                    Log?.Invoke("sent keep alive request");
                }
            }
        }

    }
}
