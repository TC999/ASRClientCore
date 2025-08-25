using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models.Enums
{
    public enum WriteMemoryMode : uint
    {
        WriteOnly,
        WriteAndExecute,
        WritePartition,
    }
}
