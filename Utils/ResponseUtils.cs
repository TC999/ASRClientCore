using ASRClientCore.Models.Enums;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ASRClientCore.Models.Enums.ResponseStatus;

namespace ASRClientCore.Utils
{
    public static class ResponseUtils
    {
        public static ResponseStatus GetResponseStatus(byte[] responseRawPacket)
        {
            if (responseRawPacket.Length < 8)
            {
                throw new ArgumentException("Response must be at least 16 bytes long.");
            }
            return BinaryPrimitives.ReadUInt32LittleEndian(responseRawPacket.AsSpan(4)) switch
            {
                0x4C494146 => Fail,
                0x59454B4F => Okey,
                _ => InvalidOrUnknown,
            };
        }
        /*public static bool CheckWritePartitionResponseStatus(byte[] responseRawPacket)
        {

        }*/
    }
}
