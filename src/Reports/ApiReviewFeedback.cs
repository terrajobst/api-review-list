using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewFeedback
    {
        public static async Task<IReadOnlyList<ApiReviewFeedback>> GetAsync(DateTimeOffset date)
        {
            static string GetApiStatus(Issue issue)
            {
                var approved = issue.Labels.Any(l => l.Name == "api-approved");
                var needsWork = issue.Labels.Any(l => l.Name == "api-needs-work");
                var isRejected = issue.Labels.Any(l => l.Name.StartsWith("api-")) &&
                                 issue.State.Value == ItemState.Closed;

                var isApi = approved || needsWork || isRejected;

                if (!isApi)
                    return null;

                if (approved)
                    return "Approved";

                if (isRejected)
                    return "Rejected";

                return "Needs Work";
            }

            static bool HasApiEvent(IEnumerable<EventInfo> events, DateTimeOffset date, out DateTimeOffset eventDateTime)
            {
                foreach (var e in events)
                {
                    if (e.CreatedAt.Date == date)
                    {
                        eventDateTime = e.CreatedAt;

                        switch (e.Event.Value)
                        {
                            case EventInfoState.Labeled:
                                if (e.Label.Name == "api-approved" || e.Label.Name == "api-needs-work")
                                    return true;
                                break;
                            case EventInfoState.Closed:
                                return true;
                        }
                    }
                }

                eventDateTime = default;
                return false;
            }

            static string FixTitle(string title)
            {
                var prefixes = new[]
                {
                    "api proposal:",
                    "[api proposal]",
                    "api:",
                    "[api]",
                    "feature:",
                    "feature request:",
                    "[feature]",
                    "[feature request]"
                };

                foreach (var prefix in prefixes)
                {
                    if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        title = title.Substring(prefix.Length).Trim();
                }

                return title;
            }

            var user = "terrajobst";

            var reposText = ConfigurationManager.AppSettings["Repos"];
            var repos = OrgAndRepo.ParseList(reposText).ToArray();

            var github = GitHubClientFactory.Create();
            var results = new List<ApiReviewFeedback>();

            foreach (var (org, repo) in repos)
            {
                var request = new RepositoryIssueRequest();
                request.Filter = IssueFilter.All;
                request.State = ItemStateFilter.All;
                request.Since = date;

                var issues = await github.Issue.GetAllForRepository(org, repo, request);

                foreach (var issue in issues)
                {
                    var status = GetApiStatus(issue);
                    if (status == null)
                        continue;

                    var events = await github.Issue.Events.GetAllForIssue(org, repo, issue.Number);
                    if (!HasApiEvent(events, date, out var eventDateTime))
                        continue;

                    var comments = await github.Issue.Comment.GetAllForIssue(org, repo, issue.Number);
                    var latestFeedback = comments.LastOrDefault(c => c.User.Login == user);
                    var url = latestFeedback?.HtmlUrl ?? issue.HtmlUrl;

                    var feedback = new ApiReviewFeedback
                    {
                        Owner = org,
                        Repo = repo,
                        IssueNumber = issue.Number,
                        IssueTitle = FixTitle(issue.Title),
                        FeedbackDateTime = eventDateTime,
                        FeedbackUrl = url,
                        FeedbackStatus = status,
                        FeedbackMarkdown = latestFeedback?.Body
                    };
                    results.Add(feedback);
                }
            }

            results.Sort((x, y) => x.FeedbackDateTime.CompareTo(y.FeedbackDateTime));
            return results;
        }


        public string Owner { get; set; }
        public string Repo { get; set; }
        public int IssueNumber { get; set; }
        public string IssueTitle { get; set; }
        public DateTimeOffset FeedbackDateTime { get; set; }
        public string FeedbackUrl { get; set; }
        public string FeedbackStatus { get; set; }
        public string FeedbackMarkdown { get; set; }
    }
}
