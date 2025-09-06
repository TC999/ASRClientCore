using ASRClientCore.Models;
using ASRClientCore.Models.Enums;
using ASRClientCore.Models.Exceptions;
using ASRClientCore.Models.Interfaces;
using ASRClientCore.Models.Packet;
using ASRClientCore.Models.Payloads;
using SPRDClientCore.Utils;
using System;
using System.Net;
using System.Text;
using static ASRClientCore.Models.Enums.ResponseStatus;

namespace ASRClientCore.DeviceManager
{
    public class FlashManager : IDisposable
    {
        private readonly IRequestManager manager;
        private readonly IAsrProtocolHandler handler;
        private readonly object _lock = new object();
        private readonly Task keepDeviceAliveTask;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private const uint MaxSize = 0x1000000;
        public string DeviceInformation
        {
            get
            {
                if (field is null)
                {
                    lock (_lock)
                    {
                        manager.SendGetInformationRequest(out var deviceInfo);
                        field = deviceInfo ?? "Can't get device info";
                    }
                }
                return field;
            }
        }
        public uint Timeout { get => handler.Timeout; set => handler.Timeout = value; }
        public uint PerBlockSize { get; set { field = value < MaxSize ? value : MaxSize; } } = MaxSize;
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
                if (Okey != (response = manager.SendReadPartitionStartRequest(partName, out var size)) || size == 0) throw new BadResponseException(response);
                Log?.Invoke($"target partition: {partName}, size: {size / 1024 / 1024}MB");
                byte[] buffer = new byte[MaxSize];
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
        public void WriteMemory(ulong addr, WriteMemoryMode mode, Stream inputStream, string? partName = null)
        {
            inputStream.Position = 0;
            partName ??= string.Empty;
            ResponseStatus response;
            ulong size = (ulong)inputStream.Length;

            lock (_lock)
            {
                try
                {
                    Timeout += 5000;
                    if (Okey != (response = manager.SendWriteMemoryStartRequest(addr, size, mode, partName))) throw new BadResponseException(response);
                    Log?.Invoke($"target partition: {(partName == string.Empty ? "memory" : partName)}, addr: {(addr == ulong.MaxValue ? "WritePartitionAddr" : "0x" + addr.ToString("x"))}, size: {size / 1024 / 1024}MB, mode: {mode}");
                    byte[] buf = new byte[MaxSize];
                    AsrReceivedPacket packet;
                    long nextSize = Math.Min(0x10000000, (long)size);
                    for (ulong i = 0; i < size;)
                    {
                        ulong writeSize = Math.Min(size - i, Math.Min(PerBlockSize, MaxSize));
                        inputStream.ReadExactly(buf, 0, (int)writeSize);
                        if (0 == handler.Write(buf, 0, (int)writeSize)) throw new BadResponseException(WriteError, handler.LastErrorCode);
                        if ((nextSize -= (long)writeSize) <= 0)
                        {
                            if (0 == handler.Read(buf, 0, 16)) throw new BadResponseException(WriteError, handler.LastErrorCode);
                            packet = AsrReceivedPacket.FromBytes(buf);
                            nextSize = packet.NextOperationSize;
                        }
                        i += writeSize;
                        UpdatePercentage?.Invoke((int)((double)i / size * 100));
                    }
                    if ((packet = AsrReceivedPacket.FromBytes(buf)).Status != Okey)
                    {
                        handler.Read(buf, 0, (int)packet.NextOperationSize);
                        Log?.Invoke($"failed to write {partName} partition, err: {Encoding.ASCII.GetString(buf.AsSpan(0, 32))}");
                        throw new BadResponseException(packet.Status);
                    }
                }
                finally
                {
                    Timeout -= 5000; // Restore timeout after write operation
                }
            }

        }
        public void WritePartition(string partName, ulong offset, byte[] data)
        {
            using MemoryStream ms = new MemoryStream(data);
            WritePartition(partName, offset, ms);
        }
        public void WritePartition(string partName, ulong offset, Stream inputStream)
        {
            using MemoryStream ms = new MemoryStream();
            ReadPartition(partName, ms);
            ms.Position = (long)offset;
            inputStream.CopyTo(ms);
            WritePartition(partName, ms);
        }
        public void WritePartition(string partName, Stream inputStream)
            => WriteMemory(ulong.MaxValue, WriteMemoryMode.WritePartition, inputStream, partName);
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
                    throw new BadResponseException(receivedPacket.Status);
                }
                Log?.Invoke($"successfully erased {partName}");
                handler.Timeout -= 20000;
            }
        }
        public void SetActiveSlot(SlotToSetActive slot)
            => WritePartition("misc", 0x800, slot switch
            {
                SlotToSetActive.SlotA => SlotPayload.PayloadOfSlotA,
                SlotToSetActive.SlotB => SlotPayload.PayloadOfSlotB,
                _ => throw new ArgumentOutOfRangeException(nameof(slot), "Invalid slot specified"),
            });
        public List<Partition> GetPartitionList()
        {
            ResponseStatus response;
            lock (_lock)
            {
                byte[] buf = new byte[0x10000];
                if (Okey != (response = manager.SendReadPartitionStartRequest("FULLDISK", out var size))) throw new BadResponseException(response);
                handler.Read(buf, 0, buf.Length);
                using (MemoryStream ms = new MemoryStream(buf))
                    return EfiTableUtils.GetPartitions(ms);
            }
        }
        public ulong ReadMemory(ulong address, ulong len, Stream outputStream)
        {
            ResponseStatus response;
            if (outputStream == null || !outputStream.CanWrite)
            {
                throw new ArgumentException("output stream must be writable");
            }
            lock (_lock)
            {
                if (Okey != (response = manager.SendPullMemoryRequest(address, len, out var size))) throw new BadResponseException(response);
                if (size == 0)
                {
                    Log?.Invoke("size from device is 0, return without err");
                    return 0;
                }
                Log?.Invoke($"reading memory from address {address:x} to {address + len:x}, size : {size / 1024 / 1024}MB");
                byte[] buffer = new byte[MaxSize];
                for (ulong i = 0; i < size;)
                {
                    uint readSize = (uint)Math.Min(size - i, PerBlockSize);
                    if (0 == handler.Read(buffer, 0, (int)readSize)) throw new BadResponseException(ReadError, handler.LastErrorCode);
                    outputStream.Write(buffer, 0, (int)readSize);
                    i += readSize;
                    UpdatePercentage?.Invoke((int)((double)i / size * 100));
                }
                return size;
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
        public void PowerDownDevice()
        {
            ResponseStatus response;
            lock (_lock)
            {
                if (Okey != (response = manager.SendRebootDeviceRequest(BootMode.PowerDown))) throw new BadResponseException(response);
                Log?.Invoke("powering down device");
            }
        }
        public void Repartition(List<Partition> partitionList)
        {
            ResponseStatus response;
            lock (_lock)
            {
                try
                {
                    Timeout += 20000;
                    if (partitionList == null || partitionList.Count == 0)
                    {
                        throw new ArgumentException("partition list cannot be null or empty");
                    }
                    if (Okey != (response = manager.SendRepartitionRequest(partitionList)))
                    {
                        byte[] buf = new byte[32];
                        handler.Read(buf, 0, 32);
                        Log?.Invoke(Encoding.ASCII.GetString(buf.AsSpan(0, 32)));
                        throw new BadResponseException(response);
                    }
                }
                finally
                {
                    Timeout -= 20000;
                }
            }
        }
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
