using System;
using System.IO;

namespace ApiReviewList
{
    internal static class YouTubeKeyStore
    {
        private static string GetPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Microsoft", "APIReviewList", "youtube.txt");
        }

        public static (string ClientId, string ClientSecret) GetApiKey()
        {
            var file = GetPath();
            if (!File.Exists(file))
                return (null, null);

            var lines = File.ReadAllLines(file);
            if (lines.Length != 2)
                return (null, null);

            return (lines[0], lines[1]);
        }
    }
}
