using ASRClientCore.Models.Enums;
using static ASRClientCore.Models.Enums.ResponseStatus;
using static ASRClientCore.Utils.StrToSize;

namespace ASRClientCore.Models.Packet
{
    public readonly struct AsrReceivedPacket
    {
        public const uint DefaultHeader = 0x70724455; // "UDrp"
        public readonly uint Header { get; }
        public readonly ResponseStatus Status { get { return field is Okey or Fail ? field : InvalidOrUnknown; } }
        public readonly AsrCommand CommandType { get; }
        public readonly uint NextOperationSize { get; }
        public AsrReceivedPacket(uint header, ResponseStatus status, uint cmdType, uint nextOperationSize)
        {
            Header = header;
            Status = status;
            CommandType = (AsrCommand)cmdType;
            NextOperationSize = nextOperationSize;
        }
        public override string ToString() => $"Header: {UIntToStringLE(Header)}, Status: {Status}, CommandType: {CommandType}, NextOperationSize: {NextOperationSize:x}";
        public static AsrReceivedPacket FromBytes(Span<byte> bytes)
        {
            if (bytes.Length < 16)
            {
                throw new ArgumentException("Packet must be at least 16 bytes long.");
            }
            uint header = BitConverter.ToUInt32(bytes);
            uint status = BitConverter.ToUInt32(bytes.Slice(4));
            uint type = BitConverter.ToUInt32(bytes.Slice(8));
            uint nextOperationSize = BitConverter.ToUInt32(bytes.Slice(12));
            return new AsrReceivedPacket(header, (ResponseStatus)status, type, nextOperationSize);
        }
    }
}
