using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    public static class Utils
    {
        public static string ConvertToDisplayName(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
                return "Unnamed Bundle";

            return Regex.Replace(bundleName, "(\\B[A-Z])", " $1")
                        .Replace("assets", " Assets")
                        .Split(' ')
                        .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                        .Aggregate((a, b) => a + " " + b);
        }

        public static string GetReadableFileSize(long bytes)
        {
            if (bytes <= 0) return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = (int)Math.Floor(Math.Log(bytes, 1024));
            double size = bytes / Math.Pow(1024, i);
            return $"{size:0.##} {suffixes[i]}";
        }
    }
}