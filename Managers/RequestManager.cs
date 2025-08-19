using static ASRClientCore.Models.Enums.AsrResponseStatus;
using static ASRClientCore.Models.Enums.AsrResponseStatus.ResponseStatus;
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
        public (ResponseStatus Response, string Info) SendGetInformationRequest()
        {
            Encoding.ASCII.GetBytes(GetInformation, buf);
            if (0 == handler.Write(buf, 0, 16)) return (WriteError, string.Empty);
            if (0 == handler.Read(buf, 0, 16)) return (ReadError, string.Empty);
            var status = GetResponseStatus(buf);
            int size = BinaryPrimitives.ReadInt32LittleEndian(buf.AsSpan(12));
            if (0 == handler.Read(buf, 0, size)) return (ReadError, string.Empty);
            var info = Encoding.ASCII.GetString(buf.AsSpan(0, 16));
            Log?.Invoke(info);
            Array.Clear(buf,0,16);
            return (status, info);
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
            if ((response = GetResponseStatus(buf)) != Okey) return response;

            if (0 == handler.Read(buf, 0, 8)) return ReadError;
            size = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(0, 8));

            Log?.Invoke(Encoding.ASCII.GetString(buf.AsSpan(0, 16)));
            return response;
        }

    }
}
