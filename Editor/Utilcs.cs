using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace com.github.xuuxiaolan.crassetbundlebuilder
{
    public static class Utils
    {
        public static string ConvertToDisplayName(string bundleName)
        {
            return Regex.Replace(bundleName, "(\\B[A-Z])", " $1")
                        .Replace("assets", " Assets")
                        .Split(' ')
                        .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                        .Aggregate((a, b) => a + " " + b);
        }

        public static string GetReadableFileSize(long bytes)
        {
            if (bytes <= 0) return "N/A";

            string[] suffixes = { "B", "KB", "MB", "GB" };
            int i;
            double doubleBytes = bytes;

            for (i = 0; i < suffixes.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                doubleBytes = bytes / 1024.0;
            }

            return string.Format("{0:0.##} {1}", doubleBytes, suffixes[i]);
        }
    }
}