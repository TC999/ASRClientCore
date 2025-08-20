using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ASRClientCore.Utils
{
    public static class StrToSize
    {
        public static ulong StringToSize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 0;
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                string hexPart = s.Substring(2);
                if (uint.TryParse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hexValue))
                    return hexValue;
                throw new ArgumentException($"无法解析十六进制数：{s}", nameof(s));
            }

            var units = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase)
    {
        { "B", 1UL },
        { "",  1UL },
        { "K", 1024UL },
        { "KB", 1024UL },
        { "M", 1024UL * 1024 },
        { "MB", 1024UL * 1024 },
        { "G", 1024UL * 1024 * 1024 },
        { "GB", 1024UL * 1024 * 1024 },
        { "T", 1024UL * 1024 * 1024 * 1024 },
        { "TB", 1024UL * 1024 * 1024 * 1024 },
    };

            var match = Regex.Match(s, @"^(?<num>\d+)(?<unit>[a-zA-Z]{0,2})$");
            if (!match.Success)
                throw new ArgumentException($"格式不正确：{s}", nameof(s));

            string numPart = match.Groups["num"].Value;
            string unitPart = match.Groups["unit"].Value.ToUpperInvariant();

            if (!ulong.TryParse(numPart, out ulong number))
                throw new ArgumentException($"无法解析数字部分：{numPart}", nameof(s));
            if (!units.TryGetValue(unitPart, out ulong multiplier))
                throw new ArgumentException($"不支持的单位：{unitPart}", nameof(s));

            ulong result = number * multiplier;
            if (result > ulong.MaxValue)
                throw new OverflowException($"结果超过最大值：{result}");

            return result;
        }
        public static string UIntToStringLE(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
