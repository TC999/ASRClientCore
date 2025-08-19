using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using static ASRClientCore.Models.Enums.AsrResponseStatus.ResponseStatus;

namespace ASRClientCore.Models.Enums
{
    public static class AsrResponseStatus
    {
        public enum ResponseStatus
        {
            Okey,
            Fail,
            InvalidOrUnknown,
            WriteError,
            ReadError,
        }
        public static ResponseStatus GetResponseStatus(byte[] responseRawPacket)
        {
            if (responseRawPacket.Length < 8)
            {
                throw new ArgumentException("Response must be at least 16 bytes long.");
            }
            switch (BinaryPrimitives.ReadUInt32LittleEndian(responseRawPacket.AsSpan(4)))
            {
                default: return InvalidOrUnknown;
                case 0x4C494146: return Fail;
                case 0x59454B4F: return Okey;
            }
        }
    }
}
