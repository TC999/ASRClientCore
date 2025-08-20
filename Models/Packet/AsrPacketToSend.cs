using ASRClientCore.Models.Enums;
using System.Data;
using static ASRClientCore.Utils.StrToSize;


namespace ASRClientCore.Models.Packet
{
    public struct AsrPacketToSend
    {
        public const uint DefaultHeader = 0x6D634455; // "UDcm"
        public readonly uint Header { get; }
        public AsrCommand Command { get; set; }
        public uint OptionalPart1 { get; set; }
        public uint OptionalPart2 { get; set; }
        public AsrPacketToSend(AsrCommand command, uint optionalOption1 = 0, uint optionalOption2 = 0)
        {
            Header = DefaultHeader; // "UDcm"
            Command = command;
            OptionalPart1 = optionalOption1;
            OptionalPart2 = optionalOption2;
        }
        public byte[] ToBytes()
        {
            byte[] bytes = new byte[16];
            ToBytes(bytes);
            return bytes;
        }
        public override string ToString() => 
            $"Header: {UIntToStringLE(Header)}, Command: {Command}, OptionalPart1: {OptionalPart1:x}, OptionalPart2: {OptionalPart2:x}";

        public void ToBytes(Span<byte> destination)
        {
            if (destination.Length < 16)
            {
                throw new ArgumentException("Destination span must be at least 16 bytes long.");
            }
            BitConverter.TryWriteBytes(destination.Slice(0), Header);
            BitConverter.TryWriteBytes(destination.Slice(4), (uint)Command);
            BitConverter.TryWriteBytes(destination.Slice(8), OptionalPart1);
            BitConverter.TryWriteBytes(destination.Slice(12), OptionalPart2);
        }
    }
}
