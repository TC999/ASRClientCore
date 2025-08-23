using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models.Enums
{
    public enum BootMode : uint
    {
        Normal,
        DisconnectUSB,
        Normal1,
        BootLoader,
        Calibration,
        Ata,
        CurrentTest,
        UDL,
        PowerDown = 0x14,
        ColdRebootToNormal
    }
}
