using static ASRClientCore.Utils.ResponseUtils;
using static ASRClientCore.Models.Enums.ResponseStatus;
using ASRClientCore.Models.Enums;
using ASRClientCore.Models.Interfaces;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using static ASRClientCore.Models.Commands.AsrCommandList;

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

            Encoding.ASCII.GetBytes(GetInformation, buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;

            var status = GetResponseStatus(buf);
            int size = BinaryPrimitives.ReadInt32LittleEndian(buf.AsSpan(12));

            if (0 == handler.Read(buf, 0, size)) return ReadError;
            deviceInfo = Encoding.ASCII.GetString(buf.AsSpan(0, 16));

            Log?.Invoke(deviceInfo);
            Array.Clear(buf, 0, 16);
            return status;
        }
        public ResponseStatus SendReadPartitionRequest(string partName, out ulong size)
        {
            size = 0;
            ResponseStatus response;

            Encoding.ASCII.GetBytes(ReadPartition, buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            Array.Clear(buf, 0, 16);

            Encoding.ASCII.GetBytes(partName, buf);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;
            if ((response = GetResponseStatus(buf)) != Okey)
            {
                Log?.Invoke($"failed to read {partName} partition, {partName} partition may not exist");
                return response;
            }

            if (0 == handler.Read(buf, 0, 8)) return ReadError;
            size = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(0, 8));

            Log?.Invoke(Encoding.ASCII.GetString(buf.AsSpan(0, 16)));
            Array.Clear(buf, 0, 16);
            return response;
        }
        /*public ResponseStatus SendWritePartitionRequest(string partName)
        {
            ResponseStatus response;

            Encoding.ASCII.GetBytes(WritePartition, buf);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(12), 32);
            if (0 == handler.Write(buf, 0, 16)) return WriteError;
            if (0 == handler.Read(buf, 0, 16)) return ReadError;

            if ((response = GetResponseStatus(buf)) != Okey && BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(8)) != 0x56434552) // "RECV"
            {
                Log?.Invoke("invalid response for writepartition request.");
                return response;
            }

            Array.Clear(buf, 0, 16);
        }*/
        public void SendWritePartitionRequest(string partName)
        {
            ResponseStatus response;

            Encoding.ASCII.GetBytes(WritePartition, buf);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(12), 32);
            if (0 == handler.Write(buf, 0, 16)) return ;
            if (0 == handler.Read(buf, 0, 16)) return ;

            if ((response = GetResponseStatus(buf)) != Okey && BinaryPrimitives.ReadUInt32LittleEndian(buf.AsSpan(8)) != 0x56434552) // "RECV"
            {
                Log?.Invoke("invalid response for writepartition request.");
                return ;
            }
            Console.WriteLine(Encoding.ASCII.GetString(buf.AsSpan(0, 16)));

            Array.Clear(buf, 0, 16);

            Encoding.ASCII.GetBytes(partName, buf);
            if (0 == handler.Write(buf, 0, 16)) return;
            if (0 == handler.Read(buf, 0, 16)) return;

            Console.WriteLine(Encoding.ASCII.GetString(buf.AsSpan(0, 16)));

        }
    }
}
