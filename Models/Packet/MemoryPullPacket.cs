using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models.Packet
{
    public struct MemoryPullPacket
    {
        public uint Address { get; set; }
        public uint Length { get; set; }
        public ulong Reserved { get; set; }
        public MemoryPullPacket(uint address, uint length, ulong reserved = 0)
        {
            Address = address;
            Length = length;
            Reserved = reserved;
        }
        public byte[] ToBytes()
        {
            byte[] bytes = new byte[16];
            ToBytes(bytes);
            return bytes;
        }
        public void ToBytes(Span<byte> destination)
        {
            if (destination.Length < 16)
            {
                throw new ArgumentException("Destination span must be at least 16 bytes long.");
            }
            BitConverter.TryWriteBytes(destination.Slice(0), Address);
            BitConverter.TryWriteBytes(destination.Slice(4), Length);
            BitConverter.TryWriteBytes(destination.Slice(8), Reserved);
        }
    }
}
