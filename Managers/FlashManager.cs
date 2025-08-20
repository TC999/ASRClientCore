using ASRClientCore.Models.Interfaces;
using static ASRClientCore.Models.Enums.ResponseStatus;
using System;
using ASRClientCore.Models.Exceptions;
using ASRClientCore.Models.Enums;
using ASRClientCore.Models.Packet;

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
        public string DeviceInformation
        {
            get
            {
                if (field is null)
                {
                    manager.SendGetDeviceInfoRequest(out var deviceInfo);
                    field = deviceInfo ?? "Can't get device info";
                }
                return field;
            }
        }
        public uint Timeout { get => handler.Timeout; set => handler.Timeout = value; }
        public uint PerBlockSize { get; set { field = value < MaxReadSize ? value : MaxReadSize; } } = MaxReadSize;
        public int KeepAliveInterval { get; set; } = 5000;
        public event Action<string>? Log;
        public event Action<int>? UpdatePercentage;
        public FlashManager(IRequestManager manager)
        {
            this.manager = manager;
            this.handler = manager.Handler;
            manager.SendGetInformationRequest(out var _);
            keepDeviceAliveTask = Task.Run(() => KeepDeviceAlive(cts.Token));
        }
        public void Dispose()
        {
            cts.Cancel();
            handler.Dispose();
            try
            {
                keepDeviceAliveTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { }
            GC.SuppressFinalize(this);
        }
        public void ReadPartition(string partName, Stream outputStream)
        {
            ResponseStatus response;
            if (outputStream == null || !outputStream.CanWrite)
            {
                throw new ArgumentException("output stream must be writable");
            }
            lock (_lock)
            {
                if (Okey != (response = manager.SendReadPartitionRequest(partName, out var size)) || size == 0) throw new BadResponseException(response);
                Log?.Invoke($"target partition : {partName}, size : {size / 1024 / 1024}MB");
                byte[] buffer = new byte[MaxReadSize];
                for (ulong i = 0; i < size;)
                {
                    uint readSize = (uint)Math.Min(size - i, PerBlockSize);
                    if (0 == handler.Read(buffer, 0, (int)readSize)) throw new BadResponseException(ReadError, handler.LastErrorCode);
                    outputStream.Write(buffer, 0, (int)readSize);
                    i += readSize;
                    UpdatePercentage?.Invoke((int)((double)i / size * 100));
                }
            }
        }
        public void ErasePartition(string partName)
        {
            ResponseStatus response;
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(partName))
                {
                    throw new ArgumentException("partition name cannot be null or empty");
                }
                byte[] buf = new byte[16];
                if (Okey != (response = manager.SendErasePartitionRequest(partName))) throw new BadResponseException(response);
                handler.Timeout += 20000;
                if (0 == handler.Read(buf, 0, 16)) throw new BadResponseException(ReadError, handler.LastErrorCode); // Read the response packet
                AsrReceivedPacket receivedPacket = AsrReceivedPacket.FromBytes(buf);
                if (receivedPacket.Status != Okey)
                {
                    Log?.Invoke($"failed to erase {partName} partition");
                    throw new BadResponseException(receivedPacket.Status);
                }
                Log?.Invoke($"successfully erased {partName}");
                handler.Timeout -= 20000;
            }
        }
        public void RebootDeviceToCustomMode(BootMode bootMode)
        {
            ResponseStatus response;
            lock (_lock)
            {
                if (Okey != (response = manager.SendRebootDeviceRequest(bootMode))) throw new BadResponseException(response);
                Log?.Invoke($"rebooting device to {bootMode} mode");
            }
        }
        public void PowerdownDevice()
        {
            ResponseStatus response;
            lock (_lock)
            {
                if (Okey != (response = manager.SendPowerdownDeviceRequest())) throw new BadResponseException(response);
                Log?.Invoke("powering down device");
            }
        }
        /* public void WritePartition(string partName, Stream inputStream)
         {
             ResponseStatus response;
             if (inputStream == null || !inputStream.CanRead)
             {
                 throw new ArgumentException("input stream must be readable");
             }
             lock (_lock)
             {

             }
         }*/
        private void KeepDeviceAlive(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(KeepAliveInterval);
                lock (_lock)
                {
                    token.ThrowIfCancellationRequested();
                    manager.SendGetInformationRequest(out var _);
                }
            }
        }

    }
}
