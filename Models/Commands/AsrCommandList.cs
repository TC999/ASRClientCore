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
    }
}
