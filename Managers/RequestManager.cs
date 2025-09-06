using static ASRClientCore.Models.Packet.AsrReceivedPacket;
using static ASRClientCore.Models.Enums.ResponseStatus;
using ASRClientCore.Models.Enums;
using ASRClientCore.Models.Interfaces;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using static ASRClientCore.Models.Enums.AsrCommand;
using ASRClientCore.Models.Packet;
using System.Reflection.Emit;
using ASRClientCore.Models;

namespace ASRClientCore.DeviceManager
{
    class RequestManager : IRequestManager
    {
        private readonly IAsrProtocolHandler handler;
        private readonly byte[] buf = new byte[64];
        public event Action<string>? Log;
        public IAsrProtocolHandler Handler => handler;
        public uint Timeout
        {
            get => handler.Timeout;
            set => handler.Timeout = value;
        }
        public RequestManager(IAsrProtocolHandler asrUsbDevice)
        {
            handler = asrUsbDevice;
        }
        public ResponseStatus SendGetInformationRequest(out string? deviceInfo)
        {
            deviceInfo = null;
            AsrPacketToSend packet = new AsrPacketToSend(CmdGetInformation);
            AsrReceivedPacket receivedPacket;

            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;

            receivedPacket = FromBytes(buf);

            if ((int)receivedPacket.NextOperationSize > 0)
                if (0 == handler.Read(buf, 0, (int)receivedPacket.NextOperationSize)) return ReadError;
            deviceInfo = Encoding.ASCII.GetString(buf.AsSpan(0, 16));

            Log?.Invoke(deviceInfo);
            Array.Clear(buf, 0, buf.Length);
            return receivedPacket.Status;
        }
        public ResponseStatus SendGetDeviceInfoRequest(out string? deviceInfo)
        {
            deviceInfo = null;
            AsrPacketToSend packet = new AsrPacketToSend(CmdGetDeviceInfo);
            AsrReceivedPacket receivedPacket;
            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            receivedPacket = FromBytes(buf);
            if (0 == handler.Read(buf, 0, (int)receivedPacket.NextOperationSize)) return ReadError;
            deviceInfo = Encoding.UTF8.GetString(buf.AsSpan(0, (int)receivedPacket.NextOperationSize));
            Array.Clear(buf, 0, buf.Length);
            return receivedPacket.Status;
        }
        public ResponseStatus SendReadPartitionStartRequest(string partName, out ulong size)
        {
            size = 0;
            AsrPacketToSend packet = new AsrPacketToSend(CmdReadPartition);
            AsrReceivedPacket receivedPacket;

            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            Array.Clear(buf, 0, 16);

            Encoding.ASCII.GetBytes(partName, buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;

            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            if ((receivedPacket = FromBytes(buf)).Status != Okey)
            {
                handler.Read(buf, 0, 32);
                Console.WriteLine(Encoding.ASCII.GetString(buf.AsSpan(0, 32)));
                return PartitionNotFound;
            }

            if (0 == handler.Read(buf, 0, 8)) return ReadError;
            size = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(0, 8));

            Log?.Invoke(Encoding.ASCII.GetString(buf.AsSpan(0, 16)));
            return receivedPacket.Status;
        }
        public ResponseStatus SendErasePartitionRequest(string partName)
        {
            AsrPacketToSend packet = new AsrPacketToSend(CmdErasePartition);
            AsrReceivedPacket receivedPacket;

            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            Array.Clear(buf, 0, 16);
            Encoding.ASCII.GetBytes(partName, buf);
            if (0 == handler.Write(buf, 0, 32)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            receivedPacket = FromBytes(buf);
            Log?.Invoke($"erase partition {partName} started : {receivedPacket.Status}");
            Array.Clear(buf, 0, 32);
            return receivedPacket.Status;
        }
        public ResponseStatus SendPullMemoryRequest(ulong address, ulong len, out ulong size)
        {
            size = 0;
            AsrPacketToSend packet = new AsrPacketToSend(CmdReadMemory);
            AsrReceivedPacket receivedPacket;
            packet.ToBytes(buf);

            if (0 == handler.Write(buf, 0, 16)) return WriteError;

            MemoryPullPacket pullPacket = new MemoryPullPacket(address, len);
            pullPacket.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;

            receivedPacket = FromBytes(buf);

            if (receivedPacket.Status != Okey)
            {
                Array.Clear(buf, 0, 16);
                handler.Read(buf, 0, 32);
                return receivedPacket.Status;
            }

            if (0 == handler.Read(buf, 0, 8)) return ReadError;

            size = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(0, 8));
            if (size == 0) handler.Read(buf, 0, 8);
            size = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(0, 8));

            return receivedPacket.Status;
        }
        public ResponseStatus SendRebootDeviceRequest(BootMode bootMode)
        {
            AsrPacketToSend packet = new AsrPacketToSend(CmdRebootDevice, (uint)bootMode);
            AsrReceivedPacket receivedPacket;
            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            receivedPacket = FromBytes(buf);
            return receivedPacket.Status;
        }
        public ResponseStatus SendPowerDownDeviceRequest()
        {
            AsrPacketToSend packet = new AsrPacketToSend(CmdPowerdownDevice);
            AsrReceivedPacket receivedPacket;
            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            receivedPacket = FromBytes(buf);
            Array.Clear(buf, 0, 16);
            return receivedPacket.Status;
        }
        public ResponseStatus SendWriteMemoryStartRequest(ulong addr, ulong size, WriteMemoryMode mode, string partName)
        {
            AsrPacketToSend packet = new AsrPacketToSend(CmdWriteMemoryStart, (uint)mode, 32);
            AsrReceivedPacket receivedPacket;
            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            if ((receivedPacket = FromBytes(buf)).Status != Okey) return receivedPacket.Status;
            Array.Clear(buf, 0, 32);
            WriteMemoryPacket writePacket = new WriteMemoryPacket(partName, size) { Address = partName == string.Empty ? ulong.MaxValue : addr };
            writePacket.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 32)) return WriteError;
            return receivedPacket.Status;
        }
        public ResponseStatus SendRepartitionRequest(List<Partition> partitionList)
        {
            byte[] partBuffer = new byte[partitionList.Count * 24];
            AsrPacketToSend packet = new AsrPacketToSend(CmdRepartitionGptPart1, (uint)CmdRepartitionGptPart2, (uint)partBuffer.Length);
            AsrReceivedPacket receivedPacket;
            packet.ToBytes(buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            Array.Clear(buf, 0, buf.Length);
            int offset = 0;
            foreach (var part in partitionList)
            {
                part.ToBytes(partBuffer.AsSpan(offset, 24));
                offset += 24;
            }
            if (0 == handler.Write(partBuffer, 0, partBuffer.Length)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            receivedPacket = FromBytes(buf);
            Array.Clear(buf, 0, buf.Length);
            return receivedPacket.Status;
        }
    }
}
