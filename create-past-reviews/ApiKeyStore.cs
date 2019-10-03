using System;
using System.IO;

namespace ApiReviewList
{
    internal static class ApiKeyStore
    {
        private static string GetPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Microsoft", "APIReviewList", "apikey.txt");
        }

        public static string GetApiKey()
        {
            var file = GetPath();
            if (!File.Exists(file))
                return null;

            return File.ReadAllText(file).Trim();
        }

        public static void SetApiKey(string key)
        {
            var file = GetPath();
            var directory = Path.GetDirectoryName(file);
            Directory.CreateDirectory(directory);
            File.WriteAllText(file, key);
        }
    }
}
