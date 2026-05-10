using NPL.SBIM.Rhi.App.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NPLEtabs.common
{
    internal class sGeneral
    {
        public enum AreaType
        {
            Slab,
            Wall,
            Unknown
        }

        public static AreaType GetAreaType(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return AreaType.Unknown;

            string s = layerName.ToUpper();

            // lấy phần cuối nếu có /
            if (s.Contains("/"))
                s = s.Split('/').Last();

            s = s.Trim();

            // 🔥 RULE CHÍNH
            if (s.StartsWith("S"))
                return AreaType.Slab;

            if (s.StartsWith("W"))
                return AreaType.Wall;

            return AreaType.Unknown;
        }

        public static string ParseSectionFromLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
                return null;

            string s = layerName.ToUpper();

            // bỏ prefix
            if (s.Contains("/"))
                s = s.Split('/').Last();

            if (s.Contains("_"))
                s = s.Split('_').Last();

            s = s.Trim();

            // ===== I =====
            var iMatch = Regex.Match(s,
                @"I[\-\s]?(\d+)[xX](\d+)[xX](\d+)[xX](\d+)");

            if (iMatch.Success)
                return $"I-{iMatch.Groups[1]}x{iMatch.Groups[2]}x{iMatch.Groups[3]}x{iMatch.Groups[4]}";

            // ===== RHS =====
            var rhsMatch = Regex.Match(s,
                @"RHS[\-\s]?(\d+)[xX](\d+)[xX](\d+)");

            if (rhsMatch.Success)
                return $"RHS-{rhsMatch.Groups[1]}x{rhsMatch.Groups[2]}x{rhsMatch.Groups[3]}";

            // ===== RECT (QUAN TRỌNG: GIỮ PREFIX) =====
            var rectMatch = Regex.Match(s,
                @"([BC])[\-\s]?(\d+)[xX](\d+)");

            if (rectMatch.Success)
                return $"{rectMatch.Groups[1]}{rectMatch.Groups[2]}x{rectMatch.Groups[3]}";

            // ===== CIRCLE =====
            var circleMatch = Regex.Match(s,
                @"([C])[\-\s]?(\d+)");

            if (circleMatch.Success)
                return $"{circleMatch.Groups[1]}{circleMatch.Groups[2]}";

            return null;
        }
        public static Log EnsureLog(List<Log> logs, string objectId, string type)
        {
            var log = logs.FirstOrDefault(x => x.ObjectID == objectId && x.Type == type);

            if (log == null)
            {
                log = new Log
                {
                    ObjectID = objectId,
                    Type = type,
                    Time = DateTime.Now
                };
                logs.Add(log);
            }

            return log;
        }
    }
    internal static class PointUtils
    {
        public static (double x, double y, double z) ParsePoint(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new Exception("Point null");

            // remove ký tự thừa
            input = input.Replace("(", "")
                         .Replace(")", "")
                         .Replace(" ", "");

            var parts = input.Split(',');

            if (parts.Length != 3)
                throw new Exception($"Sai format point: {input}");

            double x = double.Parse(parts[0]);
            double y = double.Parse(parts[1]);
            double z = double.Parse(parts[2]);

            // ⚠️ nếu Rhino export mm → convert sang m
            x /= 1000.0;
            y /= 1000.0;
            z /= 1000.0;

            return (x, y, z);
        }
    }
}
