using System.Diagnostics;

namespace ApiReviewList
{
    internal static class Shell
    {
        public static void Execute(string text)
        {
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = text
            });
        }
    }
}
