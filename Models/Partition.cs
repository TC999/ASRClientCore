using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRClientCore.Models
{
    public struct Partition
    {
        public ulong Size;
        public string Name;
        public int IndicesToMB;
        public static string[] CommonPartitions { get; } = {
            "splloader","prodnv", "miscdata", "recovery", "misc", "trustos", "trustos_bak",
       "sml", "sml_bak", "uboot", "uboot_bak", "logo","logo_1" ,"logo_2","logo_3","logo_4","logo_5","logo_6","fbootlogo",
     "l_fixnv1", "l_fixnv2", "l_runtimenv1", "l_runtimenv2",
     "gpsgl", "gpsbd", "wcnmodem", "persist", "l_modem",
     "l_deltanv", "l_gdsp", "l_ldsp", "pm_sys", "boot",
     "system",  "cache","vendor", "uboot_log", "userdata","dtb","socko","vbmeta","vbmeta_bak","vbmeta_system",
     "trustos_a", "trustos_b", "sml_a", "sml_b", "teecfg","teecfg_a", "teecfg_b",
     "uboot_a", "uboot_b", "gnssmodem_a", "gnssmodem_b", "wcnmodem_a",
     "wcnmodem_b", "l_modem_a", "l_modem_b", "l_deltanv_a", "l_deltanv_b",
     "l_gdsp_a", "l_gdsp_b", "l_ldsp_a", "l_ldsp_b", "l_agdsp_a", "l_agdsp_b",
    "l_cdsp_a", "l_cdsp_b", "pm_sys_a", "pm_sys_b", "boot_a", "boot_b",
    "vendor_boot_a", "vendor_boot_b", "dtb_a", "dtb_b", "dtbo_a", "dtbo_b",
    "super", "socko_a", "socko_b", "odmko_a", "odmko_b", "vbmeta_a", "vbmeta_b",
    "metadata", "sysdumpdb", "vbmeta_system_a", "vbmeta_system_b",
    "vbmeta_vendor_a", "vbmeta_vendor_b", "vbmeta_system_ext_a",
    "vbmeta_system_ext_b", "vbmeta_product_a","nr_fixnv1","nr_fixnv2","nr_runtimenv1","nr_runtimenv2"
    ,"nr_pmsys","nr_agdsp","nr_modem","nr_v3phy","nr_nrphy","nr_nrdsp1","nr_nrdsp2",
    "nr_deltanv","m_raw","m_data","m_webui","ubipac", "vbmeta_product_b","user_partition"
};
        public override string ToString()
        {
            decimal sizeValue = (decimal)Size / (1UL << IndicesToMB);
            return $"name : {Name} , size : {Math.Round(sizeValue, 0)}";
        }
        public void ToBytes(Span<byte> bytes)
        {
            if (bytes.Length < 24) throw new ArgumentException();
            BitConverter.TryWriteBytes(bytes, Size << (20 - IndicesToMB));
            Encoding.ASCII.GetBytes(Name, bytes.Slice(8));
        }
    }
}
