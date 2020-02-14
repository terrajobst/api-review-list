using System;

namespace ApiReviewList
{
    internal static class GitHubIssueHelpers
    {
        public static string FixTitle(string title)
        {
            var prefixes = new[]
            {
                    "api proposal",
                    "[api proposal]",
                    "api",
                    "[api]",
                    "proposal",
                    "[proposal]",
                    "feature",
                    "feature request",
                    "[feature]",
                    "[feature request]",
                    ":"
                };

            foreach (var prefix in prefixes)
            {
                if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    title = title.Substring(prefix.Length).Trim();
            }

            return title;
        }

        public static string GetMarkdownLink(string owner, string repo, int id, string url, string title)
        {
            var fixedTitle = FixTitle(title);
            return $"[{owner}/{repo}#{id}: {fixedTitle}]({url})";
        }

        public static string GetHtmlLink(string owner, string repo, int id, string url, string title)
        {
            var fixedTitle = FixTitle(title);
            return $"<a href=\"{url}\">{owner}/{repo}#{id}: {fixedTitle}</a>";
        }
    }
}
