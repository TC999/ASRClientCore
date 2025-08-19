using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models.Commands
{
    public static class AsrCommandList
    {
        public static string GetInformation = "UDcmGETI";
        public static string GetDeviceInfo = "UDcmGTI2";
        public static string GetDeviceKeyHash = "UDcmRDKH";
        public static string ExecuteAtAddress = "UDcmRUNI";
        public static string WriteOtpKey = "UDcmWOTP";
        public static string RepartitionGpt = "UDcmFMTSFGPT";
        public static string WritePartition = "UDcmDWLD";
        public static string ReadPartition = "UDcmUPLD";
        public static string ErasePartition = "UDcmERPT";
        public static string ReadFlash = "UDcmPULL";
        public static string RebootDevice = "UDcmREBT";
        public static string PowerDownDevice = "UDcmPOWD";
       /* public static byte[] GetCommandBytes(string command)
        {
            byte[] commandBytes = new byte[16];
            GetCommandBytes(command, commandBytes);
            return commandBytes;
        }
        public static void GetCommandBytes(string command, byte[] buffer)
        {
            if (buffer == null || buffer.Length < 16)
            {
                throw new ArgumentException("Buffer must be at least 16 bytes long.");
            }
            if (string.IsNullOrEmpty(command) || command.Length != 8)
            {
                throw new ArgumentException("Command must be exactly 8 characters long.");
            }
            Encoding.ASCII.GetBytes(command, 0, command.Length, buffer, 0);
        }*/
    }
}
