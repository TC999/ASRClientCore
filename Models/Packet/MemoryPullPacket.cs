using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models.Packet
{
    public struct MemoryPullPacket
    {
        public ulong Address { get; set; }
        public ulong Length { get; set; }
        public MemoryPullPacket(ulong address, ulong length)
        {
            Address = address;
            Length = length;
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
            BitConverter.TryWriteBytes(destination.Slice(8), Length);
        }
    }
}
