namespace ASRClientCore.Models.Enums
{
    public enum AsrCommand : uint
    {
        None,
        CmdGetInformation = 0x49544547, // "GETI"
        CmdGetDeviceInfo = 0x32495447, // "GTI2"
        CmdGetDeviceKeyHash = 0x484B4452, // "RDKH"
        CmdExecuteAtAddress = 0x494E5552, // "RUNI"
        CmdWriteOtpKey = 0x50544F57, // "WOTP"
        CmdRepartitionGptPart1 = 0x53544D46, // "FMTS"
        CmdRepartitionGptPart2 = 0x54504746, // "FGPT"，p1 + p2 = "FMTSFGPT"
        CmdWritePartitionStart = 0x444C5744, // "DWLD"
        CmdReadPartition = 0x444C5055, // "UPLD"
        CmdErasePartition = 0x54505245, // "ERPT"
        CmdReadFlash = 0x4C4C5550, // "PULL"
        CmdRebootDevice = 0x54424552, // "REBT"
        CmdPowerdownDevice = 0x44574F50, // "POWD"
        RepWritePartitionEnd = 0x444C5744, // "DWLD"
        RepWritePartitionBeginOrMiddle = 0x56434552, // "RECV"

        /*        public static string GetInformation = "UDcmGETI";
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
        */
    }
}
